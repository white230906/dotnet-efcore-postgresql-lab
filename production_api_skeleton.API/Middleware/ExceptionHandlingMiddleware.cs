using Microsoft.AspNetCore.Mvc;

namespace production_api_skeleton.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;
 
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request {CorrelationId} was cancelled by the client.",
                context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    exception,
                    "Response has already started for request {CorrelationId}.",
                    context.TraceIdentifier);
 
                throw;
            }
 
            await WriteProblemDetailsAsync(context, exception);
        }
    }
    
    private async Task WriteProblemDetailsAsync(
        HttpContext context,
        Exception exception)
    {
        var (statusCode, title, errorCode) = exception switch
        {
            ArgumentException => (
                StatusCodes.Status422UnprocessableEntity,
                "Validation error",
                "Request.ValidationFailed"),
 
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Business rule conflict",
                "WorkItem.InvalidState"),
 
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "Server.UnexpectedError")
        };
 
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception for request {CorrelationId}.",
                context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request {CorrelationId} failed with status {StatusCode}.",
                context.TraceIdentifier,
                statusCode);
        }
 
        var detail = statusCode >= 500 &&
                     !_environment.IsDevelopment()
            ? "An unexpected error occurred."
            : exception.Message;
        
        var problem = new ProblemDetails()
        {
            Status = statusCode,
            Title = title,
            Type = errorCode,
            Detail = detail,
            Instance = context.Request.Path
        };
 
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["correlationId"] =
            context.TraceIdentifier;
 
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
 
        await context.Response.WriteAsJsonAsync(
            problem,
            context.RequestAborted);
    }
}
    