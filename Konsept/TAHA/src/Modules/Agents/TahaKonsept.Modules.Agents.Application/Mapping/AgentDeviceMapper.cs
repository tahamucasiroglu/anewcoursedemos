using System.Text.Json;
using TahaKonsept.Modules.Agents.Domain.Entities;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.Modules.Agents.Application.Mapping;

/// <summary>
/// Cihaz varlığı ↔ API modeli dönüşümleri: <see cref="ToInfo"/> (birincil GPU = en çok VRAM'li), yetenek JSON'u okuma/yazma
/// (<see cref="ReadCapabilities"/>, <see cref="WriteCapabilities"/>; ortak <see cref="ContractJson"/> ayarlarıyla).
/// </summary>
public static class AgentDeviceMapper
{
    /// <summary>Cihazı API modeline çevirir.</summary>
    /// <param name="device">Cihaz.</param>
    /// <param name="isOnline">Canlı çevrimiçi durumu.</param>
    /// <returns>Model.</returns>
    public static AgentDeviceInfo ToInfo(AgentDevice device, bool isOnline)
    {
        ArgumentNullException.ThrowIfNull(device);
        var capabilities = ReadCapabilities(device.CapabilitiesJson);
        var gpu = capabilities?.Gpus.OrderByDescending(g => g.VramMb).FirstOrDefault();
        return new AgentDeviceInfo
        {
            Id = device.Id,
            MachineName = device.MachineName,
            AgentVersion = device.AgentVersion,
            OsDescription = device.OsDescription,
            PairedAtUtc = device.PairedAtUtc,
            LastSeenAtUtc = device.LastSeenAtUtc,
            IsOnline = isOnline,
            LanAddress = device.LanAddress,
            VideoServerPort = device.VideoServerPort,
            GpuName = gpu?.Name,
            VramMb = gpu?.VramMb,
            Capabilities = capabilities,
        };
    }

    /// <summary>Yetenekleri JSON'a çevirir.</summary>
    /// <param name="capabilities">Yetenekler.</param>
    /// <returns>JSON ya da <c>null</c>.</returns>
    public static string? WriteCapabilities(AgentCapabilities? capabilities) =>
        capabilities is null ? null : JsonSerializer.Serialize(capabilities, ContractJson.Options);

    /// <summary>Yetenek JSON'unu okur (bozuksa <c>null</c>).</summary>
    /// <param name="json">JSON.</param>
    /// <returns>Yetenekler ya da <c>null</c>.</returns>
    public static AgentCapabilities? ReadCapabilities(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AgentCapabilities>(json, ContractJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
