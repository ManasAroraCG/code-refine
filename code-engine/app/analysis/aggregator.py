from app.analysis.ai_runner import run_ai_analysis
from app.analysis.runner import run_static_analysis
from app.context.builder import build_context


def aggregate_findings(repo_path, changed_files):
    """Run static analysis first, then AI analysis seeded with those findings, combine both."""
    context = build_context(repo_path, changed_files)

    static_findings = run_static_analysis(repo_path, changed_files)
    ai_findings = run_ai_analysis(context.changed_files, static_findings)

    return static_findings + ai_findings
