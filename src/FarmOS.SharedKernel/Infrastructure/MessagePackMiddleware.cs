using System.Text;
using MessagePack;
using Microsoft.AspNetCore.Http;

namespace FarmOS.SharedKernel.Infrastructure;

/// <summary>
/// ASP.NET Core middleware that provides MessagePack content negotiation for Minimal APIs.
///
/// Minimal APIs don't support MVC InputFormatters/OutputFormatters, so this middleware
/// bridges the gap by converting between MessagePack wire format and JSON at the HTTP boundary.
///
/// Uses MessagePack's built-in format-level converters (ConvertToJson/ConvertFromJson)
/// to avoid lossy object round-tripping.
///
/// Request flow:  application/x-msgpack body → ConvertToJson → JSON body for model binding
/// Response flow: JSON response → ConvertFromJson → application/x-msgpack body
/// </summary>
public sealed class MessagePackMiddleware
{
    private const string MsgPackContentType = "application/x-msgpack";
    private readonly RequestDelegate _next;

    public MessagePackMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // ─── Request: convert incoming MessagePack body to JSON for model binding ───
        if (context.Request.ContentType?.Contains(MsgPackContentType, StringComparison.OrdinalIgnoreCase) == true
            && (context.Request.ContentLength ?? 1) > 0)
        {
            var msgPackBytes = await ReadRequestBodyAsync(context.Request);
            var json = MessagePackSerializer.ConvertToJson(msgPackBytes, MsgPackOptions.Standard);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            context.Request.Body = new MemoryStream(jsonBytes);
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = jsonBytes.Length;
        }

        // ─── Response: convert outgoing JSON to MessagePack if client accepts it ───
        var acceptsMsgPack = context.Request.Headers.Accept
            .Any(a => a?.Contains(MsgPackContentType, StringComparison.OrdinalIgnoreCase) == true);

        if (!acceptsMsgPack)
        {
            await _next(context);
            return;
        }

        // Capture the response body written by the endpoint
        var originalBody = context.Response.Body;
        using var capturedBody = new MemoryStream();
        context.Response.Body = capturedBody;

        await _next(context);

        capturedBody.Seek(0, SeekOrigin.Begin);

        // Only convert JSON responses (leave redirects, 204s, error pages, etc. untouched)
        if (context.Response.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            var jsonString = await new StreamReader(capturedBody, Encoding.UTF8).ReadToEndAsync();
            var msgPackBytes = MessagePackSerializer.ConvertFromJson(jsonString, MsgPackOptions.Standard);

            context.Response.Body = originalBody;
            context.Response.ContentType = MsgPackContentType;
            context.Response.ContentLength = msgPackBytes.Length;
            await originalBody.WriteAsync(msgPackBytes);
        }
        else
        {
            // Non-JSON response (e.g., 404, 204) — pass through unchanged
            capturedBody.Seek(0, SeekOrigin.Begin);
            await capturedBody.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
        }
    }

    private static async Task<byte[]> ReadRequestBodyAsync(HttpRequest request)
    {
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms);
        return ms.ToArray();
    }
}
