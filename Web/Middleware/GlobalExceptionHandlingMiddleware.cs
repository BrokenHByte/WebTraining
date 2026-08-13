using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Middleware;

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

            if (context.Response.HasStarted) return;

            int statusCode = MapStatusCode(ex);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var error = new ProblemDetails
            {
                Status = statusCode,
                Title = ex.GetType().Name,
                Detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(error);
        }

        static int MapStatusCode(Exception ex)
        {
            return ex switch
            {
                ArgumentOutOfRangeException => StatusCodes.Status400BadRequest,
                BookingBeginEventException => StatusCodes.Status400BadRequest,
                BookingExceedingLimitException => StatusCodes.Status409Conflict,
                BookingNotFoundException => StatusCodes.Status404NotFound,
                EventNotFoundException => StatusCodes.Status404NotFound,
                EventValidationException => StatusCodes.Status400BadRequest,
                InsufficientExecutionStackException => StatusCodes.Status403Forbidden,
                LoginAlreadyUseException => StatusCodes.Status409Conflict,
                NoAvailableSeatsException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}