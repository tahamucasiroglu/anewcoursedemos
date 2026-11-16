using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Domain.Entities;

/// <summary>Bir bileşenin kurulum ekranındaki anlık durumu (spec 0009 Req 5, 13).</summary>
/// <param name="Component">Bileşen.</param>
/// <param name="Status">Durum.</param>
/// <param name="Failure">Hata sebebi (yalnızca <see cref="ComponentStatus.Failed"/>).</param>
/// <param name="FailedStep">Hatanın adımı (indirme / doğrulama / kurulum; 0022 bildirimine eklenir).</param>
public sealed record ComponentState(
    SetupComponent Component,
    ComponentStatus Status,
    SetupFailure Failure = SetupFailure.None,
    InstallStep? FailedStep = null);
