using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Wolverine;

namespace Kernel.Middlewares;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Exception occurred: {Message}", ex.Message);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problem = new ProblemDetails
        {
            Instance = context.TraceIdentifier,
            Type = "about:blank"
        };

        switch (exception)
        {
            case ValidationException validationException:
                problem.Status = StatusCodes.Status400BadRequest;
                problem.Title = "Dữ liệu không hợp lệ";
                problem.Detail = "Vui lòng kiểm tra lại dữ liệu gửi lên.";
                problem.Extensions["errors"] = validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).ToArray());
                break;

            case UnauthorizedAccessException unauthorizedAccessException:
                problem.Status = StatusCodes.Status401Unauthorized;
                problem.Title = "Xác thực thất bại";
                problem.Detail = "Dữ liệu người dùng có thể không hợp lệ";
                break;

            case ArgumentException argException:
                problem.Status = StatusCodes.Status400BadRequest;
                problem.Title = "Invalid Argument";
                problem.Detail = argException.Message;
                break;

            default:
                problem.Status = StatusCodes.Status500InternalServerError;
                problem.Title = "Lỗi hệ thống";
                //problem.Detail = "Đã xảy ra lỗi không mong muốn, vui lòng thử lại sau.";
                problem.Detail = $"{exception.Message}\n{exception.StackTrace}";
                break;
        }

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
