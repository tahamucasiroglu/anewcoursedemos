# Spec 0001 — talepler listesi karakterizasyonu (AC-3).
import re


def test_liste_sekiz_talep_takvim_gunu_hafta_sonu_dahil(client):
    """AC-3 / BR-3 (deliberately preserved): gun = takvim gunu, hafta sonlari dahil.
    Ayse'nin 2026-07-06–2026-07-17 talebi 12 gun (is gunu olsaydi 10)."""
    yanit = client.get("/requests")
    assert yanit.status_code == 200
    satirlar = re.findall(
        r"<tr><td>([^<]+)</td><td>([^<]+)</td><td>([^<]+)</td><td>(\d+)</td>",
        yanit.get_data(as_text=True),
    )
    assert satirlar == [
        ("Ayse Yilmaz", "2026-07-06", "2026-07-17", "12"),
        ("Mehmet Demir", "2026-11-02", "2026-11-04", "3"),
        ("Zeynep Kaya", "2026-11-09", "2026-11-09", "1"),
        ("Ahmet Celik", "2026-12-21", "2026-12-25", "5"),
        ("Ayse Yilmaz", "2026-12-28", "2026-12-30", "3"),
        ("Elif Sahin", "2026-10-05", "2026-10-07", "3"),
        ("Zeynep Kaya", "2026-09-14", "2026-09-25", "12"),
        ("Mehmet Demir", "2026-08-03", "2026-08-03", "1"),
    ]
