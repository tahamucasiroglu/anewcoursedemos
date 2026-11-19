namespace OkulYonetim.Domain.Identity;

/// <summary>
/// Faz 1 rolleri: kurucu/yönetici (kurum düzeyinde, tüm şubeleri görür), öğretmen, veli, muhasebe/idari (şube düzeyinde).
/// Bir kişinin aynı kurumda birden çok rolü olabilir (BR-1).
/// </summary>
public enum Role
{
    Founder = 1,
    Teacher = 2,
    Guardian = 3,
    Accounting = 4,
}
