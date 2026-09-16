"""Test script to run and verify the CodeRefine LangGraph workflow end-to-end."""

import json
import tempfile
from pathlib import Path

from app.workflow.graph import run_coderefine_workflow

SAMPLE_PYTHON_CODE = '''
def calculate_total(items):
    total = 0
    for item in items:
        total += item["price"]
    return total


def calculate_total_again(items):
    total = 0
    for item in items:
        total += item["price"]
    return total


def process_users(users):
    result = []

    for user in users:
        result.append(user)

    for user in users:
        print(user)

    unused_variable = "never used"

    return result
'''


def main():
    print("=" * 60)
    print("Running CodeRefine LangGraph Multi-Agent Workflow")
    print("=" * 60)

    # 1. Create a temporary workspace with sample code
    temp_dir = tempfile.mkdtemp(prefix="coderefine_test_")
    sample_file = Path(temp_dir) / "sample.py"
    sample_file.write_text(SAMPLE_PYTHON_CODE, encoding="utf-8")

    print(f"\n[Workspace] Directory: {temp_dir}")
    print(f"[Workspace] Target File: {sample_file.name}")
    print("\n--- Original Source Code ---")
    print(SAMPLE_PYTHON_CODE.strip())

    # 2. Run the LangGraph workflow
    print("\n[Executing] Running LangGraph Workflow...")
    final_state = run_coderefine_workflow(
        repo_path=temp_dir,
        changed_files=["sample.py"],
        analysis_id="demo-test-run-001",
        max_retries=2,
    )

    # 3. Print Results
    print("\n" + "=" * 60)
    print("WORKFLOW SUMMARY")
    print("=" * 60)
    print(f"Status:        {final_state.get('status')}")
    print(f"Quality Score: {final_state.get('quality_score')} / 100.0")
    print(f"Retry Count:   {final_state.get('retry_count')}")

    print("\n--- Aggregated Findings ---")
    findings = final_state.get("aggregated_findings", [])
    if not findings:
        print("  None")
    for f in findings:
        print(f"  * [{f.agent_type}] ({f.severity.upper()}) Line {f.start_line}-{f.end_line}: {f.title}")
        print(f"    Recommendation: {f.recommendation}")

    print("\n--- Fix Plan ---")
    fix_plan = final_state.get("fix_plan", [])
    if not fix_plan:
        print("  None")
    for p in fix_plan:
        print(f"  * {p.file_path}:{p.target_start_line}-{p.target_end_line} [{p.category}]")
        print(f"    Strategy: {p.proposed_strategy}")

    print("\n--- Generated Patches & Diff ---")
    combined_diff = final_state.get("combined_diff", "")
    if combined_diff:
        print(combined_diff)
    else:
        print("  No diff generated.")

    print("\n--- Verification Result ---")
    v_result = final_state.get("verification_result")
    if v_result:
        print(f"  Verdict: {v_result.verdict.upper()}")
        print(f"  Summary: {v_result.summary}")
        for c in v_result.checks:
            print(f"    - {c.check_type}: {c.status} ({c.duration_ms}ms) | {c.output}")
    else:
        print("  No verification result.")

    # 4. Check the patched file on disk
    if sample_file.exists():
        print("\n--- Resulting File on Disk After Patch ---")
        print(sample_file.read_text(encoding="utf-8").strip())

    print("\n" + "=" * 60)
    print("Workflow execution finished successfully!")
    print("=" * 60)


if __name__ == "__main__":
    main()
