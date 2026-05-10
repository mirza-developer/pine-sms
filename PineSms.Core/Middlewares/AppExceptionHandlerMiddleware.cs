using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PineSms.Core.Dtos;

namespace PineSms.Core.Middlewares;

public class AppExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AppExceptionHandlerMiddleware> logger;

    public AppExceptionHandlerMiddleware(RequestDelegate next
        , ILogger<AppExceptionHandlerMiddleware> logger)
    {
        _next = next;
        this.logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await LogRequest(context);

            await ConvertException(context, ex);
        }
    }

    private async Task LogRequest(HttpContext context)
    {
        using StreamReader reader = new(context.Request.Body, Encoding.UTF8, true, 1024, true);

        string requestBody = await reader.ReadToEndAsync();

        logger.LogInformation($"{Environment.NewLine}Request Body:{Environment.NewLine} {requestBody}{Environment.NewLine}");

        context.Request.Body.Position = 0;
    }

    private Task ConvertException(HttpContext context, Exception exception)
    {
        logger.LogWarning(exception, $"PineSms.Api ex => {exception.Message}");

        HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;

        context.Response.ContentType = "application/json";

        var result = string.Empty;

#if DEBUG
        Debugger.Break();
#endif

        httpStatusCode = HttpStatusCode.InternalServerError;

        result = JsonSerializer.Serialize(new ResponseDto()
        {
            Success = false,
            Message = "An unexpected error occurred. Please try again later."
        });

        return context.Response.WriteAsync(result);
    }
}
