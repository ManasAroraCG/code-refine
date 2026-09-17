import subprocess
from typing import List, Union
from fastapi import APIRouter, HTTPException

from app.analysis.runner import run_static_analysis
from app.context.builder import build_context
from app.models.schemas import (
    AgentFindingDto,
    AnalyzeRequest,
    AnalyzeResponse,
    CloneRequest,
    CodeContext,
    ContextRequest,
    Finding,
    PatchRequest,
    PatchResult,
    VerificationResult,
    VerifyRequest,
    WorkflowResponse,
    WorkspaceResponse,
)
from app.patch.applier import apply_patch
from app.sandbox.verification import run_verification
from app.workflow.graph import run_coderefine_workflow
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


@router.post("/static-analysis", response_model=List[Finding])
def static_analysis(request: ContextRequest):
    return run_static_analysis(request.repo_path, request.changed_files)


@router.post("/analyze", response_model=AnalyzeResponse)
def analyze(request: AnalyzeRequest):
    """Integrated LangGraph Multi-Agent analysis returning backend-compatible AnalyzeResponse."""
    repo_path = request.repo_path or request.repository_path or ""
    changed_files = request.changed_files or []
    analysis_id = request.analysis_id or "analysis-default"

    final_state = run_coderefine_workflow(
        repo_path=repo_path,
        changed_files=changed_files,
        analysis_id=analysis_id,
    )

    findings: List[AgentFindingDto] = []
    for f in final_state.get("aggregated_findings", []):
        findings.append(
            AgentFindingDto(
                agent=f.agent_type,
                file=f.file or f.file_path or "",
                start_line=f.start_line,
                end_line=f.end_line,
                severity=f.severity,
                category=f.category,
                title=f.title,
                description=f.description,
                recommendation=f.recommendation,
                confidence=f.confidence,
                fix_available=f.fix_available,
            )
        )

    return AnalyzeResponse(
        analysis_id=analysis_id,
        findings=findings,
        quality_score=final_state.get("quality_score", 100.0),
    )


@router.post("/workspace/run", response_model=WorkflowResponse)
@router.post("/workflow/run", response_model=WorkflowResponse)
def run_workflow_endpoint(request: AnalyzeRequest):
    """Run full LangGraph workflow and return complete state including fix plan, patches, verification, repair history, and improvement PR URL."""
    repo_path = request.repo_path or request.repository_path or ""
    changed_files = request.changed_files or []
    analysis_id = request.analysis_id or "analysis-full-run"

    final_state = run_coderefine_workflow(
        repo_path=repo_path,
        changed_files=changed_files,
        analysis_id=analysis_id,
        repo_url=request.repo_url or "",
        pr_number=request.pr_number,
        base_branch=request.base_branch,
    )

    return WorkflowResponse(
        analysis_id=analysis_id,
        status=final_state.get("status", "completed"),
        quality_score=final_state.get("quality_score", 100.0),
        findings=final_state.get("aggregated_findings", []),
        fix_plan=final_state.get("fix_plan", []),
        generated_patches=final_state.get("generated_patches", []),
        combined_diff=final_state.get("combined_diff", ""),
        verification_result=final_state.get("verification_result"),
        retry_count=final_state.get("retry_count", 0),
        repair_history=final_state.get("repair_history", []),
        improvement_branch=final_state.get("improvement_branch"),
        improvement_pr_url=final_state.get("improvement_pr_url"),
    )


@router.post("/apply-patch", response_model=PatchResult)
def apply_patch_route(request: PatchRequest):
    return apply_patch(request)


@router.post("/verify", response_model=VerificationResult)
def verify(request: VerifyRequest):
    return run_verification(request.repo_path)
