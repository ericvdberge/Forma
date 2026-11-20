using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace Forma.Api.Abstractions;

/// <summary>
/// Answers a JSON body that doesn't match its schema (unknown block type, misspelled or missing property, bad enum)
/// with the same <c>400</c> validation problem as a failed rule, keyed by the JSON path of the offending value,
/// e.g. <c>document.sections[0].blocks[2].props.levl</c>.
/// </summary>
public class JsonBodyExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException { InnerException: JsonException json })
        {
            return false;
        }

        var path = json.Path is { Length: > 0 } p ? p.TrimStart('$').TrimStart('.') : "body";
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new HttpValidationProblemDetails(new Dictionary<string, string[]>
            {
                [path.Length > 0 ? path : "body"] = [Describe(json)],
            })
            {
                Status = StatusCodes.Status400BadRequest,
            },
        });
    }

    /// <summary>The serializer's message without its trailing "Path: … | LineNumber: …" part.</summary>
    private static string Describe(JsonException json)
    {
        var message = json.Message;
        var cut = message.IndexOf(" Path: ", StringComparison.Ordinal);
        return cut > 0 ? message[..cut] : message;
    }
}
