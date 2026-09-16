from typing import List
from app.models.schemas import Finding
from app.workflow.graph import run_coderefine_workflow


def aggregate_findings(repo_path: str, changed_files: List[str]) -> List[Finding]:
    """Execute the integrated LangGraph workflow and return aggregated findings."""
    result = run_coderefine_workflow(repo_path=repo_path, changed_files=changed_files)
    return result.get("aggregated_findings", [])
