using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Ports;

/// <summary>Audit kaydı ekler (yalnızca ekleme; aynı kayıt işlemiyle birlikte yazılır). Okuma ekranı sonraki spec'lerde.</summary>
public interface IAuditLog
{
    void Add(AuditEntry entry);
}
