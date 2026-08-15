import os

import httpx
import jwt
from fastapi import Depends, HTTPException, Request
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from jwt import PyJWKClient

# Mirrors api/Api/Auth (JWT bearer validated against an Auth0 authority) and
# libs/OpenFGA/OpenFgaAuthorizationHandler.cs (OpenFGA relation check) - this
# API has a single fixed check instead of a per-route relation/object, so
# there's no need for the .NET version's AuthorizationRequirement machinery.
AUTH_AUTHORITY = os.environ.get("AUTH_AUTHORITY", "https://oliverflecke.eu.auth0.com")
AUTH_AUDIENCE = os.environ.get("AUTH_AUDIENCE")

OPENFGA_HOST = os.environ.get("OPENFGA_HOST", "http://localhost:6080")
OPENFGA_STORE_ID = os.environ.get("OPENFGA_STORE_ID", "")
OPENFGA_MODEL_ID = os.environ.get("OPENFGA_MODEL_ID", "")

_jwks_client = PyJWKClient(f"{AUTH_AUTHORITY}/.well-known/jwks.json", cache_keys=True)
_bearer = HTTPBearer()


async def require_system_admin(
    request: Request,
    credentials: HTTPAuthorizationCredentials = Depends(_bearer),
) -> str:
    """FastAPI dependency verifying the bearer token belongs to a user who is
    an admin on system:core in OpenFGA. Wire in globally via FastAPI(dependencies=[...])
    so every endpoint is gated."""
    token = credentials.credentials
    try:
        signing_key = _jwks_client.get_signing_key_from_jwt(token)
        claims = jwt.decode(
            token,
            signing_key.key,
            algorithms=["RS256"],
            audience=AUTH_AUDIENCE,
            options={"verify_aud": AUTH_AUDIENCE is not None},
        )
    except jwt.PyJWTError as e:
        raise HTTPException(status_code=401, detail=f"invalid token: {e}") from e

    user = claims.get("sub")
    if not user:
        raise HTTPException(status_code=401, detail="token missing sub claim")

    http_client: httpx.AsyncClient = request.app.state.http_client
    response = await http_client.post(
        f"{OPENFGA_HOST}/stores/{OPENFGA_STORE_ID}/check",
        json={
            "authorization_model_id": OPENFGA_MODEL_ID,
            "tuple_key": {
                "user": f"user:{user}",
                "relation": "admin",
                "object": "system:core",
            },
        },
    )
    response.raise_for_status()
    if not response.json().get("allowed"):
        raise HTTPException(status_code=403, detail="not an admin on system:core")

    return user
