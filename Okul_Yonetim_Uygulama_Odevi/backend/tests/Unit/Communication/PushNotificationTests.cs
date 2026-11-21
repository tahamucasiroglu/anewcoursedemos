using OkulYonetim.Application.Ports;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Communication;

/// <summary>
/// Spec 0016 review tur 1 R1-4: metinli bildirim yalnızca <see cref="PushNotificationKind.Appointment"/> türünde üretilebilir (öğretmene
/// randevu bildirimi). Başka her türde metin kurucuda da <c>with { Text = … }</c> ile de reddedilir — duyuru, mesaj, okul bildirimi içeriğini
/// kaynağından okur; serbest metin (ad, konu) bildirim yoluna, ileride gerçek push ile cihaza (backlog B-6) giremez. Enum'a yeni tür
/// eklenirse bu test onu da kapsar.
/// </summary>
public sealed class PushNotificationTests
{
    public static TheoryData<PushNotificationKind> OtherKinds() =>
        [.. Enum.GetValues<PushNotificationKind>().Where(k => k != PushNotificationKind.Appointment)];

    [Theory]
    [MemberData(nameof(OtherKinds))]
    public void Constructor_TextOnNonAppointmentKind_Throws(PushNotificationKind kind)
    {
        Should.Throw<ArgumentException>(() => new PushNotification(Guid.NewGuid(), Guid.NewGuid(), kind, Guid.NewGuid(), "Ayşe Yılmaz — gizli konu"));
    }

    [Theory]
    [MemberData(nameof(OtherKinds))]
    public void With_TextOnNonAppointmentKind_Throws(PushNotificationKind kind)
    {
        PushNotification push = new(Guid.NewGuid(), Guid.NewGuid(), kind, Guid.NewGuid());

        push.Text.ShouldBeNull();
        Should.Throw<ArgumentException>(() => push with { Text = "Ayşe Yılmaz — gizli konu" });
    }

    [Fact]
    public void Constructor_AppointmentKind_CarriesText()
    {
        PushNotification push = new(Guid.NewGuid(), Guid.NewGuid(), PushNotificationKind.Appointment, Guid.NewGuid(), "Yeni randevu talebi — 26.11.2026 14:00");

        push.Text.ShouldBe("Yeni randevu talebi — 26.11.2026 14:00");
        (push with { Text = "Randevu iptal edildi — 26.11.2026 14:00" }).Text.ShouldBe("Randevu iptal edildi — 26.11.2026 14:00");
    }
}
