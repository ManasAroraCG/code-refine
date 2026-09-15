from app.analysis.ai_stub import analyze_file_with_ai
from app.models.schemas import Finding


def run_ai_analysis(changed_files_context, static_findings=None):
    """TEMPORARY stand-in for Dev 2's AI agent service - runs each changed file through Gemini."""
    static_findings = static_findings or []
    findings = []

    for file_ctx in changed_files_context:
        file_static_findings = [f for f in static_findings if f.file_path == file_ctx.path]

        try:
            raw_items = analyze_file_with_ai(file_ctx.path, file_ctx.content, file_static_findings)
        except Exception as exc:
            print(f"AI analysis failed for {file_ctx.path}: {exc}")
            continue

        for item in raw_items:
            category = item.get("category", "unknown")
            findings.append(Finding(
                agent_type=f"ai:{category}",
                file_path=file_ctx.path,
                start_line=item.get("start_line", 0),
                end_line=item.get("end_line", 0),
                category=category,
                severity=item.get("severity", "low"),
                title=item.get("title", ""),
                description=item.get("description", ""),
            ))

    return findings
