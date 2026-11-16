using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Application.Abstract;

/// <summary>Kalıcı kurulum durumu deposu (spec 0009 Req 15, 19–20).</summary>
public interface ISetupStateStore
{
    /// <summary>Durumu okur; yoksa boş durum.</summary>
    /// <returns>Durum.</returns>
    SetupState Load();

    /// <summary>Durumu yazar.</summary>
    /// <param name="state">Durum.</param>
    void Save(SetupState state);
}
