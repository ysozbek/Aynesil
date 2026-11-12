using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;

namespace Aynesil.Application.Common.Validation;

/// <summary>
/// Yazma işleminin üst kaydı (rol, şablon, program, menü öğesi …) kiracının kendisine ait olmalı.
/// Alt tabloların (role_permission, *_translation, şablon bölüm/madde …) çoğunda corporation_id yoktur;
/// handler bu yüzden yazmadan önce üst kaydın sahibini doğrular. Veritabanı aynı kuralı alt tablolardaki
/// üst kayda bağlı RLS politikalarıyla da uygular (V44).
///
/// <list type="bullet">
///   <item>Kendi kaydı → izin verilir.</item>
///   <item>Global/platform kaydı (corporation_id IS NULL; tüm kiracılara görünür) → 403
///         <see cref="MsgKeys.Rule.PlatformRecordReadOnly"/>: değişiklik bütün kiracıları etkilerdi.</item>
///   <item>Başka kiracının kaydı → 404 (RLS altında zaten görünmez; varlığı sızdırılmaz).</item>
/// </list>
/// </summary>
public static class TenantOwnership
{
    public static void EnsureWritable(Guid? ownerCorporationId, Guid corporationId, string entity)
    {
        if (ownerCorporationId == corporationId) return;

        if (ownerCorporationId is null)
            throw new ForbiddenAccessException(MsgKeys.Rule.PlatformRecordReadOnly, new EnumValue("Entity", entity));

        throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", entity));
    }
}
