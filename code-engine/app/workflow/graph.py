import uuid
from typing import List, Optional
from langgraph.graph import END, START, StateGraph

from app.analysis.runner import run_static_analysis
from app.context.builder import build_context
from app.models.schemas import CodeContext, Finding
from app.workflow.agents import (
    dead_code_agent_node,
    efficiency_agent_node,
    redundancy_agent_node,
    security_agent_node,
)
from app.workflow.aggregator import aggregator_node
from app.workflow.patcher import apply_patch_node, patch_generator_node
from app.workflow.planner import fix_planner_node
from app.workflow.pr_publisher import commit_and_push_node, create_pr_node
from app.workflow.repair import retry_patch_generator_node
from app.workflow.state import CodeRefineState
from app.workflow.verifier import route_verification, verification_node


def prepare_input_node(state: CodeRefineState) -> dict:
    """Ensure state defaults and containers are clean before fan-out."""
    return {
        "analysis_id": state.get("analysis_id") or f"run-{uuid.uuid4().hex[:8]}",
        "repo_path": state.get("repo_path", ""),
        "repo_url": state.get("repo_url", ""),
        "pr_number": state.get("pr_number"),
        "base_branch": state.get("base_branch"),
        "changed_files": state.get("changed_files", []),
        "code_context": state.get("code_context") or CodeContext(),
        "static_findings": state.get("static_findings", []),
        "ai_findings": [],
        "aggregated_findings": [],
        "quality_score": state.get("quality_score", 0.0),
        "fix_plan": [],
        "generated_patches": [],
        "combined_diff": "",
        "verification_result": None,
        "retry_count": 0,
        "max_retries": state.get("max_retries", 2),
        "repair_feedback": None,
        "repair_history": [],
        "improvement_branch": None,
        "improvement_pr_url": None,
        "status": "prepared",
    }


def build_graph():
    """Build and compile the complete CodeRefine LangGraph StateGraph."""
    graph = StateGraph(CodeRefineState)

    # Add all workflow nodes
    graph.add_node("prepare_input", prepare_input_node)
    graph.add_node("redundancy_agent", redundancy_agent_node)
    graph.add_node("efficiency_agent", efficiency_agent_node)
    graph.add_node("dead_code_agent", dead_code_agent_node)
    graph.add_node("security_agent", security_agent_node)
    graph.add_node("aggregator", aggregator_node)
    graph.add_node("fix_planner", fix_planner_node)
    graph.add_node("patch_generator", patch_generator_node)
    graph.add_node("apply_patch", apply_patch_node)
    graph.add_node("verification", verification_node)
    graph.add_node("retry_patch_generator", retry_patch_generator_node)
    graph.add_node("commit_and_push", commit_and_push_node)
    graph.add_node("create_pr", create_pr_node)

    # Entry to input preparation
    graph.add_edge(START, "prepare_input")

    # Run specialized AI agents sequentially to avoid bursting through Groq TPM limits.
    graph.add_edge("prepare_input", "redundancy_agent")
    graph.add_edge("redundancy_agent", "efficiency_agent")
    graph.add_edge("efficiency_agent", "dead_code_agent")
    graph.add_edge("dead_code_agent", "security_agent")
    graph.add_edge("security_agent", "aggregator")

    # Linear pipeline: Aggregator -> Planner -> Patch Generator -> Apply Patch -> Verification
    graph.add_edge("aggregator", "fix_planner")
    graph.add_edge("fix_planner", "patch_generator")
    graph.add_edge("patch_generator", "apply_patch")
    graph.add_edge("apply_patch", "verification")

    # Conditional Routing:
    #   - retry: loop back to retry_patch_generator (then apply_patch -> verification again)
    #   - end  : route to commit_and_push regardless of verdict (always push for human review)
    graph.add_conditional_edges(
        "verification",
        route_verification,
        {
            "end": "commit_and_push",
            "retry": "retry_patch_generator",
        },
    )
    # Loop back to apply_patch after retry patch generation
    graph.add_edge("retry_patch_generator", "apply_patch")

    # Final publisher pipeline
    graph.add_edge("commit_and_push", "create_pr")
    graph.add_edge("create_pr", END)

    return graph.compile()


def run_coderefine_workflow(
    repo_path: str,
    changed_files: List[str],
    analysis_id: Optional[str] = None,
    static_findings: Optional[List[Finding]] = None,
    max_retries: int = 2,
    repo_url: str = "",
    pr_number: Optional[int] = None,
    base_branch: Optional[str] = None,
) -> dict:
    """Helper to construct context, run static tools, and execute the compiled graph."""
    run_id = analysis_id or f"run-{uuid.uuid4().hex[:8]}"

    # Extract structured code context
    code_context = build_context(repo_path, changed_files)

    # Run deterministic static analysis if not already supplied
    if static_findings is None:
        try:
            static_findings = run_static_analysis(repo_path, changed_files)
        except Exception as e:
            print(f"Static analysis failed: {e}. Proceeding with empty static findings.")
            static_findings = []

    initial_state: CodeRefineState = {
        "analysis_id": run_id,
        "repo_path": repo_path,
        "repo_url": repo_url,
        "pr_number": pr_number,
        "base_branch": base_branch,
        "changed_files": changed_files,
        "code_context": code_context,
        "static_findings": static_findings,
        "ai_findings": [],
        "aggregated_findings": [],
        "quality_score": 0.0,
        "fix_plan": [],
        "generated_patches": [],
        "combined_diff": "",
        "verification_result": None,
        "retry_count": 0,
        "max_retries": max_retries,
        "repair_feedback": None,
        "repair_history": [],
        "improvement_branch": None,
        "improvement_pr_url": None,
        "status": "initialized",
    }

    compiled_graph = build_graph()
    final_state = compiled_graph.invoke(initial_state)
    return final_state
