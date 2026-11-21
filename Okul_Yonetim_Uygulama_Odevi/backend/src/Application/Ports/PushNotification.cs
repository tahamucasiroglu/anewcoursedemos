namespace OkulYonetim.Application.Ports;

/// <summary>
/// Gönderilecek tek bildirim (spec 0011): okul, alıcı kişi, tür ve kaynak kayıt (duyuru, mesaj, veli bildirimi) kimliği.
/// Ad taşımaz (gizlilik, gereksinim 8); uygulama içeriği kaynağından okur. Spec 0016: öğretmene giden randevu bildiriminin
/// kaynağı veli tarafında bir kayıt olmadığı için kısa metni (<see cref="Text"/>: yalnızca tür, gün ve saat — ad ve konu yok) taşır;
/// sahte gönderici metni de loglamaz. Spec 0016 review tur 1 R1-4: metin <b>yalnızca</b> <see cref="PushNotificationKind.Appointment"/>
/// türünde olabilir — başka türde metin verilirse (kurucuda ya da <c>with { Text = … }</c> ile) <see cref="ArgumentException"/>; diğer türler içeriği
/// kaynağından okur, serbest metin (ad, konu) bildirim yoluna giremez. Gerçek push sağlayıcısı gelince (backlog B-6) bu metin cihaza gider.
/// </summary>
public sealed record PushNotification(Guid TenantId, Guid RecipientUserId, PushNotificationKind Kind, Guid SourceId, string? Text = null)
{
    private readonly string? text = Checked(Kind, Text);

    public string? Text
    {
        get => this.text;
        init => this.text = Checked(this.Kind, value);
    }

    private static string? Checked(PushNotificationKind kind, string? text) =>
        text is null || kind == PushNotificationKind.Appointment
            ? text
            : throw new ArgumentException("Bildirim metni yalnızca randevu bildiriminde taşınır.", nameof(text));
}
