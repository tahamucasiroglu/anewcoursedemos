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
