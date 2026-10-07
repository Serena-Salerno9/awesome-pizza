using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Api.Errors;

public sealed class DomainExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
  {
    if (exception is not DomainException)
      return false;

    context.Response.StatusCode = StatusCodes.Status400BadRequest;

    return await problems.TryWriteAsync(new ProblemDetailsContext
    {
      HttpContext = context,
      Exception = exception,
      ProblemDetails =
      {
        Status = StatusCodes.Status400BadRequest,
        Title = "The request is not valid.",
        Detail = exception.Message
      }
    });
  }
}
