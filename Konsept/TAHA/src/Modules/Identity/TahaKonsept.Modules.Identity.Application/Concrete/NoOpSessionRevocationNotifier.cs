using TahaKonsept.Modules.Identity.Application.Base;

namespace TahaKonsept.Modules.Identity.Application.Concrete;

/// <summary>
/// Varsayılan bildirimci: hiçbir şey yapmaz. Agents modülü kendi bildirimcisini ekleyince ikisi birlikte çağrılır.
/// </summary>
public sealed class NoOpSessionRevocationNotifier : SessionRevocationNotifierBase
{
}
