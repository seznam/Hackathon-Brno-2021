using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CommentApi;

public class LogResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private Func<string, Exception?, string> _defaultFormatter = (state, exception) => state;

    public LogResponseMiddleware(RequestDelegate next, ILogger<LogResponseMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var bodyStream = context.Response.Body;

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        await _next(context);

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = new StreamReader(responseBodyStream).ReadToEnd();
        _logger.Log(LogLevel.Information, 1, $"RESPONSE LOG: {context.Response.StatusCode}", null, _defaultFormatter);
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(bodyStream);
    }
}
