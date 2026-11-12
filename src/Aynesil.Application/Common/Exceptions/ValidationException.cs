using Aynesil.Shared;
using FluentValidation.Results;

namespace Aynesil.Application.Common.Exceptions;

public class ValidationException : Exception
{
    /// <summary>Kodu olmayan (elle üretilmiş) hatalar için varsayılan kod.</summary>
    public const string DefaultErrorCode = "Error.InvalidValue";

    public ValidationException() : base("Validation failed.") { }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        var list = failures.ToList();
        Errors = list
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
        ErrorDetails = list
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => new ErrorDetail(
                        string.IsNullOrEmpty(f.ErrorCode) ? DefaultErrorCode : f.ErrorCode,
                        f.ErrorMessage))
                    .ToArray());
    }

    /// <summary>Alan → render edilmiş mesajlar (mevcut istemci uyumu için aynen korunur).</summary>
    public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();

    /// <summary>Alan → { code, message } (kodlu biçim).</summary>
    public IDictionary<string, ErrorDetail[]> ErrorDetails { get; } = new Dictionary<string, ErrorDetail[]>();
}
