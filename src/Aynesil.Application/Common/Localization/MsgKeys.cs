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
        public const string ValidationFailed = "Error.ValidationFailed";
        public const string InvalidRequest = "Error.InvalidRequest";
        public const string InvalidValue = "Error.InvalidValue";
        public const string InvalidArgument = "Error.InvalidArgument";
        public const string NotFound = "Error.NotFound";
        public const string Forbidden = "Error.Forbidden";
        public const string Unauthorized = "Error.Unauthorized";
        public const string TenantSessionInvalid = "Error.TenantSessionInvalid";
        public const string Concurrency = "Error.Concurrency";
        public const string DuplicateKey = "Error.DuplicateKey";
        public const string ReferenceMissing = "Error.ReferenceMissing";
        public const string ReferenceInUse = "Error.ReferenceInUse";
        public const string RequestCancelled = "Error.RequestCancelled";
        public const string Unexpected = "Error.Unexpected";
    }
}
