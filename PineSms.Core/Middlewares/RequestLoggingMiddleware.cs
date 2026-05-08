using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PineSms.Core.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.Value.ToLower().Contains("account"))
        {
            Stream originalBody = context.Request.Body;
            
            using MemoryStream buffer = new();
            
            await context.Request.Body.CopyToAsync(buffer);
           
            buffer.Position = 0;

            string requestBody = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();

            if (!string.IsNullOrEmpty(requestBody))
            {
                _logger.LogInformation($"{Environment.NewLine}PineSms.Api => Path:{Environment.NewLine} {context.Request.Path.Value}{Environment.NewLine}Request Body:{Environment.NewLine} {requestBody}{Environment.NewLine}");
            }

            buffer.Position = 0;
            
            context.Request.Body = buffer;
            
            await _next(context);
            
            context.Request.Body = originalBody;
          
            return;
        }

        await _next(context);
    }

}
