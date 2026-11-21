# Spec 0002 — bekleyen talep iptali. Referans tarih 2026-11-21, her testten once seed() (conftest).
import re

import app

HATA = "Yalniz bekleyen talepler iptal edilebilir."


def _talep(no):
    return [r for r in app.Db.requests if r["id"] == no][0]


def _iptal(client, no):
    return client.post("/requests/%s/cancel" % no)


# --- servis --------------------------------------------------------------------------------


def test_servis_bekleyeni_iptal_eder_digerlerini_degistirmez():
    """AC-2, AC-5, AC-6 (servis): yalniz Beklemede -> Iptal Edildi; digerleri False, degisiklik yok."""
    assert app.svc.cancel_request(2) is True
    assert _talep(2)["status"] == "Iptal Edildi"
    assert app.svc.cancel_request(2) is False  # ikinci kez
    assert app.svc.cancel_request(1) is False  # Onaylandi
    assert app.svc.cancel_request(6) is False  # Reddedildi
    assert app.svc.cancel_request(999) is False  # yok
    assert [r["status"] for r in app.Db.requests] == [
        "Onaylandi", "Iptal Edildi", "Beklemede", "Beklemede",
        "Beklemede", "Reddedildi", "Reddedildi", "Reddedildi",
    ]


# --- HTTP ----------------------------------------------------------------------------------


def test_iptal_dugmesi_yalniz_bekleyen_satirlarda(client):
    """AC-1: seed'de 2, 3, 4, 5 numarali taleplerde dugme var; 1, 6, 7, 8'de yok."""
    html = client.get("/requests").get_data(as_text=True)
    numaralar = [int(n) for n in re.findall(r'action="/requests/(\d+)/cancel"', html)]
    assert numaralar == [2, 3, 4, 5]
    assert html.count(">Iptal</button>") == 4


def test_bekleyen_talep_iptal_edilir(client):
    """AC-2: no 2 -> Iptal Edildi, diger alanlar ayni, /requests'e donulur, listede dugmesiz gorunur."""
    once = dict(_talep(2))
    yanit = _iptal(client, 2)
    assert yanit.status_code == 302
    assert yanit.headers["Location"].endswith("/requests")
    sonra = _talep(2)
    assert sonra["status"] == "Iptal Edildi"
    assert {k: v for k, v in sonra.items() if k != "status"} == {k: v for k, v in once.items() if k != "status"}
    assert len(app.Db.requests) == 8
    html = client.get("/requests").get_data(as_text=True)
    assert "<td>Mehmet Demir</td><td>2026-11-02</td><td>2026-11-04</td><td>3</td><td>Aile ziyareti</td>" in html
    assert "Iptal Edildi</b>" in html
    assert 'action="/requests/2/cancel"' not in html


def test_iptal_sonrasi_dashboard_3_1_3(client):
    """AC-3: iptal edilen talep hicbir kutuda sayilmaz."""
    _iptal(client, 2)
    html = client.get("/").get_data(as_text=True)
    sayilar = dict(re.findall(r"(Bekleyen|Onaylanan|Reddedilen): <b>(\d+)</b>", html))
    assert sayilar == {"Bekleyen": "3", "Onaylanan": "1", "Reddedilen": "3"}


def test_iptal_bakiyeyi_degistirmez(client):
    """AC-4 / BR-4: Ayse'nin bekleyen talebi (no 5) iptal edilince bakiye tablosu aynen kalir."""
    desen = r"<tr><td>([^<]+)</td><td>[^<]+</td><td>(-?\d+)</td><td>(-?\d+)</td><td>(-?\d+)</td></tr>"
    once = re.findall(desen, client.get("/").get_data(as_text=True))
    assert _iptal(client, 5).status_code == 302
    sonra = re.findall(desen, client.get("/").get_data(as_text=True))
    assert ("Ayse Yilmaz", "26", "12", "14") in sonra
    assert sonra == once


def test_bekleyen_olmayan_talep_iptal_edilemez(client):
    """AC-5: Onaylandi (1), Reddedildi (6) ve ikinci kez iptal -> 400, mesaj, yonlendirme yok."""
    _iptal(client, 3)
    for no, durum in [(1, "Onaylandi"), (6, "Reddedildi"), (3, "Iptal Edildi")]:
        yanit = _iptal(client, no)
        assert yanit.status_code == 400
        assert "Location" not in yanit.headers
        assert HATA in yanit.get_data(as_text=True)
        assert _talep(no)["status"] == durum


def test_olmayan_veya_gecersiz_numara(client):
    """AC-6: 999 ve 'abc' -> 400, ayni mesaj, hicbir talep degismez."""
    once = [dict(r) for r in app.Db.requests]
    for no in ["999", "abc"]:
        yanit = _iptal(client, no)
        assert yanit.status_code == 400
        assert "Location" not in yanit.headers
        assert HATA in yanit.get_data(as_text=True)
    assert app.Db.requests == once


def test_iptal_dugmesi_form_yalniz_numara(client):
    """AC-8: dugme bir <button type="submit">, metni 'Iptal'; form yalniz talep numarasini tasir."""
    html = client.get("/requests").get_data(as_text=True)
    formlar = re.findall(r"<form[^>]*>.*?</form>", html, flags=re.S)
    iptal_formlari = [f for f in formlar if "/cancel" in f]
    assert len(iptal_formlari) == 4
    for f in iptal_formlari:
        assert re.fullmatch(
            r'<form method="post" action="/requests/\d+/cancel"[^>]*><button type="submit">Iptal</button></form>', f
        )


def test_get_ile_iptal_olmaz(client):
    """AC-8: adresi acmak (GET) iptal etmez."""
    yanit = client.get("/requests/2/cancel")
    assert yanit.status_code == 405
    assert _talep(2)["status"] == "Beklemede"
