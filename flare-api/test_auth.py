"""Self-check for auth.require_system_admin. Run: uv run python test_auth.py"""

import asyncio
from types import SimpleNamespace
from unittest.mock import AsyncMock, patch

import jwt
from fastapi import HTTPException
from fastapi.security import HTTPAuthorizationCredentials

import auth


def _fake_request(allowed: bool) -> SimpleNamespace:
    http_client = SimpleNamespace(
        post=AsyncMock(
            return_value=SimpleNamespace(
                raise_for_status=lambda: None,
                json=lambda: {"allowed": allowed},
            )
        )
    )
    return SimpleNamespace(app=SimpleNamespace(state=SimpleNamespace(http_client=http_client)))


def _creds(token: str = "token") -> HTTPAuthorizationCredentials:
    return HTTPAuthorizationCredentials(scheme="Bearer", credentials=token)


async def _run(allowed: bool):
    with (
        patch.object(auth._jwks_client, "get_signing_key_from_jwt", return_value=SimpleNamespace(key="k")),
        patch.object(jwt, "decode", return_value={"sub": "alice"}),
    ):
        return await auth.require_system_admin(_fake_request(allowed), _creds())


def test_allowed_returns_user():
    assert asyncio.run(_run(True)) == "alice"


def test_denied_raises_403():
    try:
        asyncio.run(_run(False))
    except HTTPException as e:
        assert e.status_code == 403
    else:
        raise AssertionError("expected HTTPException")


def test_invalid_token_raises_401():
    with patch.object(auth._jwks_client, "get_signing_key_from_jwt", side_effect=jwt.PyJWTError("bad")):
        try:
            asyncio.run(auth.require_system_admin(_fake_request(True), _creds()))
        except HTTPException as e:
            assert e.status_code == 401
        else:
            raise AssertionError("expected HTTPException")


if __name__ == "__main__":
    test_allowed_returns_user()
    test_denied_raises_403()
    test_invalid_token_raises_401()
    print("ok")
