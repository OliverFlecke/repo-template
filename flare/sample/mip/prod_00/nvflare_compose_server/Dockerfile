FROM ghcr.io/astral-sh/uv:python3.14-bookworm-slim
WORKDIR /app
COPY requirements.txt requirements.txt
RUN uv pip install --system -r requirements.txt
ENTRYPOINT ["python3", "-u", "-m", "nvflare.private.fed.app.server.server_train", "-m", "/workspace", "-s", "fed_server.json", "--set", "secure_train=true", "config_folder=config", "org=maritime"]