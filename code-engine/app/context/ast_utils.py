import ast
from pathlib import Path
from typing import List
from app.models.schemas import Symbol


def extract_symbols(source: str) -> List[Symbol]:
    """Return top-level function/class defs found in a Python source string as Symbol models."""
    try:
        tree = ast.parse(source)
    except Exception:
        return []

    symbols: List[Symbol] = []
    for node in ast.walk(tree):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
            sym_type = "function"
            if isinstance(node, ast.ClassDef):
                sym_type = "class"
            elif isinstance(node, ast.AsyncFunctionDef):
                sym_type = "async_function"

            symbols.append(Symbol(
                name=node.name,
                symbol_type=sym_type,
                start_line=node.lineno,
                end_line=getattr(node, "end_lineno", node.lineno),
            ))

    return symbols


def extract_symbols_from_file(file_path: str) -> List[Symbol]:
    source = Path(file_path).read_text(encoding="utf-8", errors="ignore")
    return extract_symbols(source)
