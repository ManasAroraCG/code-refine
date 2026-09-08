# CodeRefine

> **AI writes the code. CodeRefine reviews, improves, and verifies it.**

CodeRefine is an AI-powered code quality analysis and improvement platform designed to identify and fix common quality issues in AI-generated code before it is merged.

## Problem

With the increasing use of AI coding assistants, developers often focus on whether generated code works rather than whether it is:

* Efficient
* Maintainable
* Non-redundant
* Secure
* Free of dead code
* Properly simplified
* Free from unnecessary AI-generated boilerplate or "AI slop"

CodeRefine aims to provide an automated quality-improvement layer between AI-generated code and the final merge.

## Proposed Flow

```text
GitHub PR / Branch
        ↓
Code Analysis
        ↓
Static Analysis + AI Agents
        ↓
Issue Detection
        ↓
Fix Generation
        ↓
Code Verification
        ↓
Human Review & Approval
        ↓
Improvement PR
```

## High-Level Architecture

```text
                 ┌──────────────┐
                 │   React UI   │
                 └──────┬───────┘
                        │
                        ▼
                 ┌──────────────┐
                 │ ASP.NET Core │
                 │    Backend   │
                 └──────┬───────┘
                        │
                        ▼
                 ┌──────────────┐
                 │ FastAPI +    │
                 │  LangChain   │
                 └──────┬───────┘
                        │
                        ▼
                 ┌──────────────┐
                 │ NVIDIA NIM   │
                 └──────────────┘
```

## AI Agents

The initial MVP is planned to include specialized agents for:

1. **Redundancy & Simplification**
2. **Code Efficiency**
3. **Dead Code / AI Slop**
4. **Security**

Each agent analyzes code from its own perspective and produces structured findings.

## Technology Stack

| Component       | Technology       |
| --------------- | ---------------- |
| Frontend        | React.js         |
| Primary Backend | ASP.NET Core     |
| AI Service      | Python + FastAPI |
| AI Framework    | LangChain        |
| LLM Provider    | NVIDIA NIM       |
| Database        | PostgreSQL       |
| Code Hosting    | GitHub           |
| Code Execution  | Docker           |
| CI/CD           | GitHub Actions   |

## Repository Structure

```text
code-refine/
│
├── frontend/
├── backend/
├── ai-service/
│
├── database/
│   ├── schema/
│   └── seed/
│
├── infrastructure/
│   ├── docker/
│   ├── sandbox/
│   └── deployment/
│
├── docs/
│   ├── architecture/
│   ├── api/
│   └── database/
│
└── .github/
    └── workflows/
```

## Development Setup

> **Coming soon**

The frontend, backend, and AI service will be initialized separately using their respective development tools and CLI commands.

## Project Status

🚧 **MVP — In Development**

This project is being developed as an internal company hackathon project.
