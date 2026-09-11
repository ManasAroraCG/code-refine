import subprocess

from fastapi import APIRouter, HTTPException

from app.analysis.runner import run_static_analysis
from app.context.builder import build_context
from app.models.schemas import CloneRequest, CodeContext, ContextRequest, Finding, WorkspaceResponse
from app.workspace.manager import WorkspaceManager

router = APIRouter()
workspace_manager = WorkspaceManager()


@router.post("/workspace", response_model=WorkspaceResponse)
def create_workspace(request: CloneRequest):
    try:
        if request.pr_number is not None:
            repo_path = workspace_manager.checkout_pr(request.repo_url, request.pr_number)
        else:
            if not request.branch:
                raise HTTPException(
                    status_code=400,
                    detail="branch is required when pr_number is not provided",
                )
            repo_path = workspace_manager.clone_repository(request.repo_url, request.branch)

        changed_files = []
        if request.base_branch:
            changed_files = workspace_manager.get_changed_files(repo_path, request.base_branch)

        return WorkspaceResponse(
            workspace_id=repo_path.parent.name,
            repo_path=str(repo_path),
            changed_files=changed_files,
        )
    except subprocess.CalledProcessError as exc:
        raise HTTPException(status_code=500, detail=f"git operation failed: {exc}")


@router.post("/context", response_model=CodeContext)
def get_context(request: ContextRequest):
    return build_context(request.repo_path, request.changed_files)


@router.post("/static-analysis", response_model=list[Finding])
def static_analysis(request: ContextRequest):
    return run_static_analysis(request.repo_path, request.changed_files)
