using Subify.Domain.Abstractions.Shared;
using Subify.Domain.Shared;

namespace Subify.Api.Common.Extensions;

public static class ResultExtensions
{
    public static IResult ToProblemDetail(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Başarılı sonuç hataya dönüştürülemez.");
        }

        return Results.Problem(
            statusCode: GetStatusCode(result.Error.Type),
            title: GetTitle(result.Error),
            type: $"https://api.subify.app/errors/{result.Error.Code}",
            detail: result.Error.Description,
            extensions: new Dictionary<string, object?>
            {
                { "errors", result is IValidationResult validationResult ? validationResult.Errors : [result.Error] }
            });
    }


    private static int GetStatusCode(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Locked => StatusCodes.Status423Locked,
            ErrorType.TooManyRequest => StatusCodes.Status429TooManyRequests,
            ErrorType.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
            ErrorType.InternalServerError =>StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetTitle(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => "Doğrulama Hatası",
            ErrorType.NotFound => "Kaynak Bulunamadı",
            ErrorType.Conflict => "Çakışma",
            ErrorType.Unauthorized => "Yetkilendirme Hatası",
            _ => "Bir Hata Oluştu"
        };
}
