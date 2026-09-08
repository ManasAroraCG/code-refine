# Contributing to CodeRefine

## Branching Strategy

Keep the Git workflow simple.

Create a feature branch from `main`:

```text
main
  │
  ├── feature/frontend-dashboard
  ├── feature/github-integration
  ├── feature/ai-agents
  ├── feature/docker-sandbox
  └── feature/database
```

### Branch Naming

Use:

```text
feature/<feature-name>
```

For example:

```text
feature/github-integration
feature/analysis-dashboard
feature/redundancy-agent
feature/database-schema
```

## Pull Requests

All changes should be submitted through a Pull Request.

Before creating a PR:

* Make sure the code builds successfully.
* Run relevant tests.
* Keep the PR focused on one feature/fix.
* Update documentation when necessary.
* Do not commit secrets or API keys.

## Commit Messages

Use clear and meaningful commit messages.

Examples:

```text
feat: add GitHub repository integration
feat: add redundancy analysis agent
fix: handle failed analysis requests
docs: update architecture documentation
chore: configure Docker environment
```

## Code Review

At least one team member should review a PR before it is merged into `main`.

Reviewers should check:

* Correctness
* Code quality
* Security
* Maintainability
* Unnecessary complexity
* Test coverage where applicable

## Secrets

Never commit:

```text
.env
API keys
GitHub private keys
Database passwords
Access tokens
```

Use environment variables and keep `.env.example` updated with the required variable names.

## Keep Changes Focused

Avoid combining unrelated changes in a single PR.

For example, don't combine:

```text
GitHub integration
+
Frontend redesign
+
Database changes
```

unless they are genuinely required for the same feature.

## Documentation

If a change affects:

* Architecture
* APIs
* Database structure
* Development setup

update the appropriate documentation under:

```text
docs/
```
