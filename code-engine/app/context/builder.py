from pathlib import Path
from typing import List
from app.context.ast_utils import extract_symbols
from app.models.schemas import ChangedFileContext, CodeContext


def detect_language(file_path: str) -> str:
    suffix = Path(file_path).suffix.lower()
    if suffix in (".py", ".pyw"):
        return "python"
    elif suffix in (".js", ".jsx", ".mjs"):
        return "javascript"
    elif suffix in (".ts", ".tsx"):
        return "typescript"
    elif suffix in (".cs",):
        return "csharp"
    return "text"


def build_context(repo_path: str, changed_files: List[str]) -> CodeContext:
    """Return each changed file's full content plus its top-level symbols and language."""
    results: List[ChangedFileContext] = []

    for rel_path in changed_files:
        file_path = Path(repo_path) / rel_path
        if not file_path.is_file():
            continue

        content = file_path.read_text(encoding="utf-8-sig", errors="ignore")
        lang = detect_language(rel_path)

        symbols = []
        if lang == "python":
            symbols = extract_symbols(content)

        results.append(ChangedFileContext(
            path=rel_path.replace("\\", "/"),
            content=content,
            language=lang,
            symbols=symbols,
        ))

    return CodeContext(changed_files=results)
