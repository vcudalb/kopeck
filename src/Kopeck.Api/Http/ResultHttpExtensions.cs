using Kopeck.Application.Common;
using Kopeck.Contracts;

namespace Kopeck.Api.Http;

/// <summary>The one place where use-case outcomes become HTTP status codes.</summary>
public static class ResultHttpExtensions
{
    public static IResult ToHttp(this Error error) => error.Kind switch
    {
        ErrorKind.NotFound => Results.NotFound(),
        ErrorKind.Validation => Results.BadRequest(new ApiError(error.Message)),
        ErrorKind.Conflict => Json(error, StatusCodes.Status409Conflict),
        ErrorKind.Forbidden => Json(error, StatusCodes.Status403Forbidden),
        ErrorKind.Unauthorized => Json(error, StatusCodes.Status401Unauthorized),
        _ => throw new ArgumentOutOfRangeException(nameof(error), error.Kind, null),
    };

    /// <summary>204 on success.</summary>
    public static IResult ToHttp(this Result result) => result.ToHttp(Results.NoContent);

    public static IResult ToHttp(this Result result, Func<IResult> onSuccess) => result.IsSuccess ? onSuccess() : result.Error!.ToHttp();

    /// <summary>200 with the value on success.</summary>
    public static IResult ToHttp<T>(this Result<T> result) => result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToHttp();

    /// <summary>201 with a Location header on success.</summary>
    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> location) =>
        result.IsSuccess ? Results.Created(location(result.Value), result.Value) : result.Error!.ToHttp();

    private static IResult Json(Error error, int status) => Results.Json(new ApiError(error.Message), statusCode: status);
}
