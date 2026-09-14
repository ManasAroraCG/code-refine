# CodeRefine API

Backend service for CodeRefine — analyzes GitHub repositories and pull requests, produces findings, generates AI patches, verifies them, and supports human review before opening an improvement pull request.

## Project structure

```
backend/
??? CodeRefine.Api/
    ??? Controllers/     # HTTP endpoints (repositories, pull requests, analysis, reviews)
    ??? Services/        # GitHub, Analysis, AI and Git service abstractions + implementations
    ??? Data/            # EF Core AppDbContext
    ??? Models/          # Persisted entities
    ??? DTOs/            # Request/response contracts
    ??? Enums/           # Status and severity enums
    ??? Configuration/   # Strongly typed settings
```

## Getting started

```bash
cd backend
dotnet restore
dotnet run --project CodeRefine.Api
```

The OpenAPI document is exposed at `/openapi/v1.json` in Development.

## Configuration

Set the following in `appsettings.Development.json` or user secrets:

- `ConnectionStrings:DefaultConnection` — SQLite connection string.
- `GitHub:AccessToken` — personal access token used for GitHub API calls.
- `AiService:Endpoint` / `AiService:ApiKey` — AI provider endpoint and key.

## API overview

| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/repositories` | List available repositories |
| GET | `/api/repositories/{owner}/{name}` | Get a repository |
| GET | `/api/repositories/{owner}/{name}/branches` | List branches |
| GET | `/api/repositories/{owner}/{name}/pull-requests` | List pull requests |
| GET | `/api/repositories/{owner}/{name}/pull-requests/{number}` | Get a pull request |
| GET | `/api/repositories/{owner}/{name}/pull-requests/{number}/files` | List changed files |
| POST | `/api/analysis` | Start an analysis run |
| GET | `/api/analysis/{id}` | Get analysis run details |
| GET | `/api/analysis/{id}/findings` | List findings for a run |
| POST | `/api/analysis/{id}/cancel` | Cancel a run |
| POST | `/api/reviews` | Submit a review decision for a patch |
| POST | `/api/reviews/improvement-pr/{owner}/{name}` | Open an improvement pull request |
