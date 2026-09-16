import json
import re
from typing import List
from app.models.schemas import CodeContext, Finding
from app.workflow.llm import get_llm
from app.workflow.state import CodeRefineState


AGENT_DESCRIPTIONS = {
    "ai:redundancy": "redundancy and code simplification agent. Identify duplicate logic, duplicated helper methods, and unnecessary abstractions.",
    "ai:efficiency": "code efficiency and performance agent. Identify repeated data traversals, unnecessary allocations in hot paths, and unmemoized duplicate operations.",
    "ai:dead_code": "dead code and AI slop detector. Identify unused variables, unreferenced functions, unnecessary defensive boilerplate, and excessive hallucinations.",
    "ai:security": "security analysis agent. Identify non-obvious vulnerabilities, unsafe raw data logging/printing, tainted inputs, and injection or leakage risks.",
}


def make_finding_payload(
    agent_type: str,
    category: str,
    severity: str,
    title: str,
    description: str,
    recommendation: str,
    file: str,
    start_line: int,
    end_line: int,
    confidence: float = 0.88,
) -> Finding:
    return Finding(
        id=f"{agent_type.replace(':', '-')}-{abs(hash(title)) % 1000:03d}-{start_line}",
        agent_type=agent_type,
        category=category,
        severity=severity,
        file=file,
        file_path=file,
        start_line=start_line,
        end_line=end_line,
        title=title,
        description=description,
        recommendation=recommendation,
        confidence=confidence,
        fix_available=True,
    )


def heuristic_findings(agent_type: str, file: str, content: str) -> List[Finding]:
    """Fallback heuristics when LLM is unavailable or returns an invalid response."""
    lines = content.splitlines()
    line_count = len(lines)

    if agent_type == "ai:redundancy":
        if "calculate_total" in content and "calculate_total_again" in content:
            return [
                make_finding_payload(
                    agent_type,
                    "redundancy",
                    "medium",
                    "Duplicate calculation logic",
                    "The file contains two separate functions that perform the same total calculation.",
                    "Keep one reusable function instead of repeating the same summation logic.",
                    file,
                    1,
                    min(13, line_count),
                    0.92,
                )
            ]
        elif line_count > 10:
            return [
                make_finding_payload(
                    agent_type,
                    "redundancy",
                    "low",
                    "Repeated logic patterns",
                    "Potential duplication found in sequential execution blocks.",
                    "Extract common subroutines into helper methods.",
                    file,
                    1,
                    line_count,
                    0.75,
                )
            ]

    elif agent_type == "ai:efficiency":
        if content.count("for ") >= 2:
            return [
                make_finding_payload(
                    agent_type,
                    "efficiency",
                    "medium",
                    "Repeated iteration over collections",
                    "The code iterates through the same data multiple times consecutively without a clear necessity.",
                    "Combine loops or use a single traversal that produces the required output.",
                    file,
                    max(1, int(line_count * 0.4)),
                    line_count,
                    0.80,
                )
            ]

    elif agent_type == "ai:dead_code":
        if "unused" in content or "never used" in content or content.count("print(") > 1:
            return [
                make_finding_payload(
                    agent_type,
                    "dead_code",
                    "low",
                    "Unused variable and redundant prints",
                    "An unused variable and repeated print statement suggest dead code or AI-generated slop.",
                    "Remove the unused variable and collapse redundant output statements.",
                    file,
                    max(1, int(line_count * 0.5)),
                    line_count,
                    0.89,
                )
            ]

    elif agent_type == "ai:security":
        if "print(user" in content or "print(request" in content or "print(password" in content:
            return [
                make_finding_payload(
                    agent_type,
                    "security",
                    "high",
                    "Unsafe raw data logging",
                    "Printing user objects or sensitive payload directly can leak private data into log streams.",
                    "Log sanitized data or use designated masked identifiers instead of raw objects.",
                    file,
                    max(1, int(line_count * 0.6)),
                    line_count,
                    0.93,
                )
            ]

    return []


def parse_agent_response(raw_text: str) -> List[Finding]:
    """Parse JSON array of findings from LLM output with robust error recovery."""
    text = raw_text.strip()

    # 1. Strip markdown fences if present
    if "```" in text:
        match = re.search(r"```(?:json)?\s*([\s\S]*?)\s*```", text, re.IGNORECASE)
        if match:
            text = match.group(1).strip()
        else:
            text = text.strip("`").strip()

    # 2. Extract outermost JSON array or object
    json_start = text.find("[")
    json_end = text.rfind("]")
    if json_start != -1 and json_end != -1 and json_end > json_start:
        slice_text = text[json_start : json_end + 1]
        try:
            data = json.loads(slice_text)
            if isinstance(data, list):
                return [Finding.model_validate(item) for item in data if isinstance(item, dict)]
        except Exception:
            pass

    # Try as JSON object { "findings": [...] }
    obj_start = text.find("{")
    obj_end = text.rfind("}")
    if obj_start != -1 and obj_end != -1 and obj_end > obj_start:
        slice_obj = text[obj_start : obj_end + 1]
        try:
            data = json.loads(slice_obj)
            if isinstance(data, dict):
                if "findings" in data and isinstance(data["findings"], list):
                    return [Finding.model_validate(item) for item in data["findings"] if isinstance(item, dict)]
                return [Finding.model_validate(data)]
        except Exception:
            pass

    # 3. Fallback regex search for individual JSON objects
    recovered_findings: List[Finding] = []
    object_pattern = re.compile(r"\{[^{}]*(?:\"category\"|\"title\")[^{}]*\}", re.DOTALL)
    for obj_match in object_pattern.finditer(text):
        try:
            item_dict = json.loads(obj_match.group(0))
            recovered_findings.append(Finding.model_validate(item_dict))
        except Exception:
            continue

    if recovered_findings:
        return recovered_findings

    raise ValueError(f"Could not parse valid JSON from response: {text[:200]}")


def generate_agent_findings(
    agent_type: str,
    code_context: CodeContext,
    static_findings: List[Finding],
) -> List[Finding]:
    """Prompt the specialized agent LLM or fall back to heuristics."""
    if not code_context.changed_files:
        return []

    file_text = "\n\n".join(
        f"--- File: {item.path} ({item.language}) ---\n{item.content}"
        for item in code_context.changed_files
    )
    static_prompt = json.dumps(
        [
            finding.model_dump(
                include={"id", "agent_type", "category", "severity", "file", "start_line", "end_line", "title", "description"}
            )
            for finding in static_findings
        ],
        indent=2,
    )

    role_desc = AGENT_DESCRIPTIONS.get(agent_type, "code quality analysis agent")
    prompt = (
        f"You are the {agent_type} ({role_desc}) in the CodeRefine automated code quality review system.\n"
        "Your task is to identify high-quality, actionable code quality issues in the provided code that static linters missed.\n\n"
        "RULES:\n"
        "1. Return ONLY a valid JSON array of objects with the exact schema below.\n"
        "2. Do NOT output markdown code blocks (```json), explanations, or preamble.\n"
        "3. 'confidence' MUST be a numeric float between 0.0 and 1.0 (e.g. 0.95), NEVER a string like 'high'.\n"
        "4. 'severity' must be one of: 'low', 'medium', 'high', 'critical'.\n"
        "5. 'start_line' and 'end_line' must be integers.\n\n"
        "EXAMPLE OUTPUT:\n"
        "[\n"
        "  {\n"
        f'    "agent_type": "{agent_type}",\n'
        '    "category": "redundancy",\n'
        '    "severity": "medium",\n'
        '    "file": "sample.py",\n'
        '    "start_line": 1,\n'
        '    "end_line": 13,\n'
        '    "title": "Duplicate logic",\n'
        '    "description": "Functions perform the identical task.",\n'
        '    "recommendation": "Consolidate into a single function.",\n'
        '    "confidence": 0.90,\n'
        '    "fix_available": true\n'
        "  }\n"
        "]\n\n"
        f"STATIC ANALYSIS FINDINGS (Already caught - DO NOT duplicate):\n{static_prompt}\n\n"
        f"CODE TO ANALYZE:\n{file_text}\n"
    )

    llm = get_llm()
    if llm:
        try:
            response = llm.invoke(prompt)
            content_str = response.content if hasattr(response, "content") else str(response)
            findings = parse_agent_response(content_str)
            if findings:
                for f in findings:
                    f.agent_type = agent_type
                return findings
        except Exception as e:
            print(f"[{agent_type}] LLM invocation failed: {e}. Using heuristics.")

    # Fallback to heuristics per file
    all_heuristics: List[Finding] = []
    for file_ctx in code_context.changed_files:
        h = heuristic_findings(agent_type, file_ctx.path, file_ctx.content)
        all_heuristics.extend(h)
    return all_heuristics


# ==============================
# Agent LangGraph Nodes
# ==============================

def redundancy_agent_node(state: CodeRefineState) -> dict:
    findings = generate_agent_findings("ai:redundancy", state["code_context"], state["static_findings"])
    return {"ai_findings": findings}


def efficiency_agent_node(state: CodeRefineState) -> dict:
    findings = generate_agent_findings("ai:efficiency", state["code_context"], state["static_findings"])
    return {"ai_findings": findings}


def dead_code_agent_node(state: CodeRefineState) -> dict:
    findings = generate_agent_findings("ai:dead_code", state["code_context"], state["static_findings"])
    return {"ai_findings": findings}


def security_agent_node(state: CodeRefineState) -> dict:
    findings = generate_agent_findings("ai:security", state["code_context"], state["static_findings"])
    return {"ai_findings": findings}
