using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Subify.Domain.Shared;

public class Result
{
    protected internal Result(bool isSuccess, Error error, Error[]? errors = null)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException();
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException();
        }

        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? (error == Error.None ? Array.Empty<Error>() : new[] { error });
    }

    public bool IsSuccess { get; set; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; set; }
    public Error[] Errors { get; set; }

    public static Result Success() => new Result(true, Error.None);
    public static Result Failure(Error error) => new Result(false, error);

    public static Result Failure(IEnumerable<Error> errors)
    {
        var errorList = errors as Error[] ?? errors.ToArray();

        if (errorList.Length == 0)
        {
            throw new ArgumentException("Hata listesi boş olamaz.", nameof(errors));
        }

        return new Result(false, errorList.FirstOrDefault() ?? Error.Failure("Unknown", "Error Raised", "Error Raised"), errorList);
    }

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    public static Result<T> Failure<T>(IEnumerable<Error> errors)
    {
        var errorList = errors.ToArray();
        return new Result<T>(default, false, errorList.FirstOrDefault() ?? Error.Failure("Unknown", "Error Raised", "Error Raised"), errorList);
    }
}

public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, Error error, Error[]? errors = null) : base(isSuccess, error, errors)
    {
        _value = value;
    }

    public T Value => IsSuccess
    ? _value!
    : throw new InvalidOperationException("Başarısız işlemin değerine erişilemez!");

    public static implicit operator Result<T>(T? value) => value is not null ? Success(value) : Failure<T>(Error.NullValue);
    
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
