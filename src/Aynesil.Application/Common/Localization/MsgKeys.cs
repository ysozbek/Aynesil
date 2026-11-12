using Aynesil.Domain.Common;

namespace Aynesil.Application.Common.Localization;

/// <summary>
/// Messages.resx anahtarları. Kodda anahtar literal'i kullanılmaz; tr/en kaynak testi
/// bu sınıftaki her sabitin her iki dilde de tanımlı olduğunu doğrular.
/// Anahtar aynı zamanda API yanıtındaki hata kodudur (<c>code</c>).
/// </summary>
public static class MsgKeys
{
    public static class Error
    {
        public static readonly MsgKey ValidationFailed = new("Error.ValidationFailed");
        public static readonly MsgKey InvalidRequest = new("Error.InvalidRequest");
        public static readonly MsgKey InvalidValue = new("Error.InvalidValue");
        public static readonly MsgKey InvalidArgument = new("Error.InvalidArgument");
        public static readonly MsgKey NotFound = new("Error.NotFound");
        public static readonly MsgKey Forbidden = new("Error.Forbidden");
        public static readonly MsgKey Unauthorized = new("Error.Unauthorized");
        public static readonly MsgKey TenantSessionInvalid = new("Error.TenantSessionInvalid");
        public static readonly MsgKey Concurrency = new("Error.Concurrency");
        public static readonly MsgKey DuplicateKey = new("Error.DuplicateKey");
        public static readonly MsgKey ReferenceMissing = new("Error.ReferenceMissing");
        public static readonly MsgKey ReferenceInUse = new("Error.ReferenceInUse");
        public static readonly MsgKey RequestCancelled = new("Error.RequestCancelled");
        public static readonly MsgKey Unexpected = new("Error.Unexpected");
        /// <summary>Kodsuz (geçiş dönemi) iş kuralı ihlali için yanıt kodu; metin istisnanın kendisinden gelir.</summary>
        public static readonly MsgKey BusinessRule = new("Error.BusinessRule");
    }

    public static class Validation
    {
        public static readonly MsgKey Required = new("Validation.Required");
        public static readonly MsgKey MustBeAfter = new("Validation.MustBeAfter");
        public static readonly MsgKey MustNotBeBefore = new("Validation.MustNotBeBefore");
        public static readonly MsgKey MustBeInFuture = new("Validation.MustBeInFuture");
        public static readonly MsgKey MustNotBeInFuture = new("Validation.MustNotBeInFuture");
        public static readonly MsgKey MustBePositive = new("Validation.MustBePositive");
        public static readonly MsgKey MustNotBeNegative = new("Validation.MustNotBeNegative");
        public static readonly MsgKey MustNotBeZero = new("Validation.MustNotBeZero");
        public static readonly MsgKey MustBeBetween = new("Validation.MustBeBetween");
        public static readonly MsgKey MustBeOneOf = new("Validation.MustBeOneOf");
        public static readonly MsgKey AtLeastOneOf = new("Validation.AtLeastOneOf");
        public static readonly MsgKey ExactlyOneOf = new("Validation.ExactlyOneOf");
        public static readonly MsgKey AtLeastOneItem = new("Validation.AtLeastOneItem");
        public static readonly MsgKey RequiredWhenEquals = new("Validation.RequiredWhenEquals");
        public static readonly MsgKey PercentageMax100 = new("Validation.PercentageMax100");
        public static readonly MsgKey CodeFormatLowerDotUnderscore = new("Validation.CodeFormatLowerDotUnderscore");
        public static readonly MsgKey CodeFormatSnakeCase = new("Validation.CodeFormatSnakeCase");
        public static readonly MsgKey CodeFormatSlug = new("Validation.CodeFormatSlug");
        public static readonly MsgKey CodeFormatSlugDot = new("Validation.CodeFormatSlugDot");
        public static readonly MsgKey CodeFormatUpper = new("Validation.CodeFormatUpper");
        public static readonly MsgKey CodeFormatLowerUnderscore = new("Validation.CodeFormatLowerUnderscore");
        public static readonly MsgKey UsernameFormat = new("Validation.UsernameFormat");
        public static readonly MsgKey IpAddressInvalid = new("Validation.IpAddressInvalid");
        public static readonly MsgKey JsonObjectInvalid = new("Validation.JsonObjectInvalid");
        public static readonly MsgKey PasswordMustDiffer = new("Validation.PasswordMustDiffer");
        public static readonly MsgKey VirtualRoomNoCampus = new("Validation.VirtualRoomNoCampus");
        public static readonly MsgKey SameEducatorTwice = new("Validation.SameEducatorTwice");
        public static readonly MsgKey EducatorCannotSuperviseSelf = new("Validation.EducatorCannotSuperviseSelf");
        public static readonly MsgKey DiscountTarget = new("Validation.DiscountTarget");
        public static readonly MsgKey ScholarshipPercentOrAmount = new("Validation.ScholarshipPercentOrAmount");
        public static readonly MsgKey InvalidQuestionType = new("Validation.InvalidQuestionType");
        public static readonly MsgKey DuplicateLocales = new("Validation.DuplicateLocales");
    }
}
