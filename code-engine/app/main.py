from fastapi import FastAPI

from app.api.routes import router as workspace_router

app = FastAPI()
app.router.routes.extend(workspace_router.routes)


@app.get("/")
def root():
    return {"message": "CodeRefine Code Engine is running"}