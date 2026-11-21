# Spec 0001 — dashboard karakterizasyonu (AC-1, AC-2). Referans tarih 2026-11-21 (conftest).
import re


def _html(client):
    yanit = client.get("/")
    assert yanit.status_code == 200
    return yanit.get_data(as_text=True)


def test_durum_sayilari_bekleyen4_onaylanan1_reddedilen3(client):
    """AC-1: sozlesme degeri — Bekleyen 4 · Onaylanan 1 · Reddedilen 3."""
    sayilar = dict(re.findall(r"(Bekleyen|Onaylanan|Reddedilen): <b>(\d+)</b>", _html(client)))
    assert sayilar == {"Bekleyen": "4", "Onaylanan": "1", "Reddedilen": "3"}


def test_bakiye_tablosu_bes_personel(client):
    """AC-2: Hak / Kullanilan / Kalan. BR-4: Ayse'nin bekleyen 3 gunluk talebi (2026-12-28–30)
    dusulmez (Q-2). Ahmet Celik satiri 29 Subat ise girisini de kapsar."""
    satirlar = re.findall(
        r"<tr><td>([^<]+)</td><td>[^<]+</td><td>(-?\d+)</td><td>(-?\d+)</td><td>(-?\d+)</td></tr>",
        _html(client),
    )
    assert satirlar == [
        ("Ayse Yilmaz", "26", "12", "14"),
        ("Mehmet Demir", "20", "0", "20"),
        ("Zeynep Kaya", "14", "0", "14"),
        ("Ahmet Celik", "20", "0", "20"),
        ("Elif Sahin", "0", "0", "0"),
    ]
