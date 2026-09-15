import json
import os

import google.generativeai as genai
from dotenv import load_dotenv

load_dotenv()

_MODEL_NAME = os.environ.get("GEMINI_MODEL", "gemini-3.6-flash")

PROMPT_TEMPLATE = """You are a code quality analysis agent reviewing a single file for issues \
a linter cannot catch: redundant/duplicated logic, unnecessary complexity or over-engineering, \
dead code or AI-generated boilerplate, and non-obvious security risks.

File: {path}
```
{content}
```
{existing_findings_section}
Return ONLY a JSON array (no markdown, no commentary). Each item must have:
- "category": one of "redundancy", "efficiency", "dead_code", "security"
- "severity": one of "low", "medium", "high"
- "start_line": integer
- "end_line": integer
- "title": short string
- "description": string

If there are no issues, return [].
"""

EXISTING_FINDINGS_SECTION = """
Static analysis tools already reported these issues for this file - do NOT repeat them, \
only report issues they missed:
{findings_list}
"""


def _format_existing_findings(existing_findings):
    if not existing_findings:
        return ""

    findings_list = "\n".join(
        f"- line {f.start_line}: [{f.category}] {f.title}" for f in existing_findings
    )
    return EXISTING_FINDINGS_SECTION.format(findings_list=findings_list)


def _get_model():
    api_key = os.environ.get("GEMINI_API_KEY")
    if not api_key:
        raise RuntimeError("GEMINI_API_KEY environment variable is not set")

    genai.configure(api_key=api_key)
    return genai.GenerativeModel(_MODEL_NAME)


def _extract_json(text):
    """Gemini often wraps JSON in ```json ... ``` fences - strip them before parsing."""
    text = text.strip()
    if text.startswith("```"):
        text = text.strip("`")
        if "\n" in text:
            first_line, rest = text.split("\n", 1)
            text = rest if first_line.lower().startswith("json") else text
    return json.loads(text)


def analyze_file_with_ai(file_path, content, existing_findings=None):
    """TEMPORARY stand-in for Dev 2's AI agent service - calls Gemini directly."""
    model = _get_model()
    prompt = PROMPT_TEMPLATE.format(
        path=file_path,
        content=content,
        existing_findings_section=_format_existing_findings(existing_findings),
    )
    response = model.generate_content(prompt)
    return _extract_json(response.text)
