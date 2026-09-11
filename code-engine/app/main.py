from fastapi import FastAPI

from app.api.routes import router as workspace_router

app = FastAPI()
app.include_router(workspace_router)


@app.get("/")
def root():
    return {"message": "CodeRefine Code Engine is running"}