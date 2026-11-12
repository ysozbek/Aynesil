using FluentValidation.Results;

namespace Aynesil.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException() : base("Bir veya daha fazla doğrulama hatası oluştu.") { }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();
}
