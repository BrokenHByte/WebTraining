using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WebProject.Services;

namespace WebProject.Middleware;

public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
                context.Request.Method,
                context.Request.Path,
                context.Request.Headers["x-request-id"]);
            
            if (context.Response.HasStarted)
            {
                return;
            }

            var statusCode = MapStatusCode(ex);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var error = new ProblemDetails
            {
                Status = statusCode,
                Detail = ex.Message
            };
            await context.Response.WriteAsJsonAsync(error);
        }

        static int MapStatusCode(Exception ex)
            => ex switch
            {
                ValidationException ve => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
                // TODO
            };
    }
}