import os
from pathlib import Path

from fastapi import FastAPI, HTTPException, UploadFile

# where uploaded data lands - mount a volume here (see compose.yaml)
DATA_DIR = Path(os.environ.get("DATA_DIR", "/data"))

app = FastAPI()


@app.get("/healthz")
def healthz():
	return {"status": "ok"}


@app.post("/upload")
async def upload(file: UploadFile):
	if not file.filename or not file.filename.lower().endswith(".csv"):
		raise HTTPException(400, "only .csv files are accepted")
	DATA_DIR.mkdir(parents=True, exist_ok=True)
	dest = DATA_DIR / Path(file.filename).name  # strip any path components
	dest.write_bytes(await file.read())
	return {"filename": dest.name}
