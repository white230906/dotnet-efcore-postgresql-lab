using Microsoft.AspNetCore.Http;

namespace production_api_skeleton.Presentation.Abstractions;

public abstract class ApiEndpoint
{
    protected static IResult NotFound(
        string errorCode,
        string detail)
    {
        return CreateProblem(
            StatusCodes.Status404NotFound,
            "Resource not found",
            errorCode,
            detail);
    }
 
    protected static IResult ValidationFailure(
        string errorCode,
        string detail)
    {
        return CreateProblem(
            StatusCodes.Status422UnprocessableEntity,
            "Validation error",
            errorCode,
            detail);
    }
 
    protected static IResult Conflict(
        string errorCode,
        string detail)
    {
        return CreateProblem(
            StatusCodes.Status409Conflict,
            "Conflict",
            errorCode,
            detail);
    }
    
    private static IResult CreateProblem(
        int statusCode,
        string title,
        string errorCode,
        string detail)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            type: errorCode,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = errorCode
            });
    }
}