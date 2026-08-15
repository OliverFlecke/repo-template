FROM ghcr.io/astral-sh/uv:python3.14-bookworm-slim

WORKDIR /app
COPY requirements.txt requirements.txt
RUN uv pip install --system -r requirements.txt

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
COPY client_api.py supervisord.conf ./

ENTRYPOINT ["supervisord", "-c", "/app/supervisord.conf"]