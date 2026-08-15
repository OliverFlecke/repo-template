FROM python:3.14-slim

COPY --from=ghcr.io/astral-sh/uv:latest /uv /uvx /bin/
WORKDIR /app

# Install dependencies first, in their own layer, so code-only changes don't
# invalidate the (slow) dependency install.
COPY pyproject.toml uv.lock ./
RUN uv sync --frozen --no-cache --no-install-project

COPY . .
RUN uv sync --frozen --no-cache

CMD ["/app/.venv/bin/fastapi", "run", "main.py", "--port", "80", "--host", "0.0.0.0"]
