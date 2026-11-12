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
}
