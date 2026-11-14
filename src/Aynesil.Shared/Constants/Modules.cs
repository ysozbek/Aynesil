namespace Aynesil.Shared.Constants;

/// <summary>
/// Kurum bazında açılıp kapatılabilen (yarım/isteğe bağlı) modüller. Durum `core.corporation.settings`
/// içindeki `modules` nesnesinde tutulur: <c>{"modules":{"camps":true,"surveys":false}}</c>.
/// Anahtar yoksa modül açık sayılır (geriye dönük uyum); kapatmak için açıkça <c>false</c> yazılır.
/// Bu anahtar bir platform işidir: yalnızca `devops/set_modules.py` / `new_tenant.py` yazar, kurum
/// yöneticisinin ayar güncellemesi onu değiştiremez. `iam.menu_item.feature_flag` bu kodlarla eşleşir.
/// </summary>
public static class Modules
{
    public const string Camps = "camps";
    public const string Consultancy = "consultancy";
    public const string Camera = "camera";
    public const string Surveys = "surveys";
    public const string CareTeam = "care_team";
    public const string Kpi = "kpi";

    public static readonly IReadOnlyList<string> All = [Camps, Consultancy, Camera, Surveys, CareTeam, Kpi];
}
