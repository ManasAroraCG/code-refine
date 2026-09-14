using System.Diagnostics;
using CodeRefine.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CodeRefine.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into consistent ProblemDetails responses.
/// Stack traces and inner exception details are logged but never returned to
/// the caller, so tokens and credentials cannot leak through the API.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (CodeRefineException ex)
        {
            _logger.LogWarning(ex, "Handled {ExceptionType} on {Method} {Path}",
                ex.GetType().Name, context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, ex.StatusCode, ex.Title, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client",
                context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "An unexpected error occurred while processing the request.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    }
}
