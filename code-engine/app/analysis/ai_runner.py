from typing import List, Optional
from app.models.schemas import ChangedFileContext, CodeContext, Finding
from app.workflow.agents import generate_agent_findings


def run_ai_analysis(
    changed_files_context: List[ChangedFileContext],
    static_findings: Optional[List[Finding]] = None,
) -> List[Finding]:
    """Run all 4 AI agents across changed files context."""
    static_findings = static_findings or []
    code_context = CodeContext(changed_files=changed_files_context)

    agents = ["ai:redundancy", "ai:efficiency", "ai:dead_code", "ai:security"]
    all_findings: List[Finding] = []

    for agent_type in agents:
        try:
            agent_findings = generate_agent_findings(agent_type, code_context, static_findings)
            all_findings.extend(agent_findings)
        except Exception as exc:
            print(f"Agent {agent_type} failed: {exc}")

    return all_findings
