using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Dashboard;

/// <summary>
/// Yönetici paneli (spec 0017) metinlerinin anahtarları ve okuyucusu (YAML: <c>Features/Dashboard/Resources/Locales/dashboard.tr.yaml</c>):
/// yetki ve "bulunamadı" iletileri, rapor süzgeci alan hataları, dışa aktarma sınırı, CSV sütun başlıkları, durum/yöntem adları ve
/// toplam satırı etiketleri. Metin koda gömülmez; doğrulayıcılar eşzamanlı okuyucuyu kullanır.
/// </summary>
public static class DashboardMessages
{
    public const string Section = "Dashboard";
    public const string Forbidden = "Forbidden";
    public const string BranchNotFound = "BranchNotFound";
    public const string FromRequired = "FromRequired";
    public const string ToRequired = "ToRequired";
    public const string RangeInverted = "RangeInverted";
    public const string RangeTooLong = "RangeTooLong";
    public const string MinAbsentInvalid = "MinAbsentInvalid";
    public const string PageInvalid = "PageInvalid";
    public const string KindInvalid = "KindInvalid";
    public const string TooManyRows = "TooManyRows";
    public const string ColStudent = "ColStudent";
    public const string ColSchoolNumber = "ColSchoolNumber";
    public const string ColBranch = "ColBranch";
    public const string ColClassroom = "ColClassroom";
    public const string ColAbsent = "ColAbsent";
    public const string ColLate = "ColLate";
    public const string ColDate = "ColDate";
    public const string ColReceiptNo = "ColReceiptNo";
    public const string ColAmount = "ColAmount";
    public const string ColMethod = "ColMethod";
    public const string ColStatus = "ColStatus";
    public const string ColRecordedBy = "ColRecordedBy";
    public const string ColPlan = "ColPlan";
    public const string ColNet = "ColNet";
    public const string ColPaid = "ColPaid";
    public const string ColRemaining = "ColRemaining";
    public const string ColOverdue = "ColOverdue";
    public const string ColOldestDue = "ColOldestDue";
    public const string StatusValid = "StatusValid";
    public const string StatusCancelled = "StatusCancelled";
    public const string MethodCash = "MethodCash";
    public const string MethodBankTransfer = "MethodBankTransfer";
    public const string MethodCardPos = "MethodCardPos";
    public const string TotalRow = "TotalRow";
    public const string TotalValidRow = "TotalValidRow";
    public const string TotalMethodRow = "TotalMethodRow";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> DashboardTextAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string DashboardText(this ILocalizationService localization, string key) =>
        localization.DashboardTextAsync(key).GetAwaiter().GetResult();
}
