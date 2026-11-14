namespace Aynesil.Application.Common.Interfaces;

/// <summary>Geçerli kurumun modül açık/kapalı durumu (`core.corporation.settings.modules`).</summary>
public interface IModuleAccess
{
    /// <summary>Modül açık mı? Anahtar yoksa açık (bkz. <see cref="Aynesil.Shared.Constants.Modules"/>).</summary>
    Task<bool> IsEnabledAsync(string module, CancellationToken ct = default);

    /// <summary>Tüm bilinen modüllerin etkin durumu.</summary>
    Task<IReadOnlyDictionary<string, bool>> GetAllAsync(CancellationToken ct = default);
}
