# Spec 0004 — gecersiz tarihte gorunur hata. KALICI repro testleri (bug-fix disiplini):
# duzeltmeden once kirmizi gosterildi, regresyon korumasi olarak suite'te kalir.
import pytest

import app

TARIH_HATASI = (
    "HATA: Tarihler gecersiz. Baslangic ve bitis gecerli bir tarih olmali; "
    "bitis baslangictan once olamaz."
)


def _gonder(client, veri):
    return client.post("/new", data=veri)


def _beklenen_tarih_hatasi(yanit):
    assert yanit.status_code == 400
    assert "Location" not in yanit.headers
    html = yanit.get_data(as_text=True)
    assert TARIH_HATASI in html
    assert "<h2>LeaveManager</h2>" in html  # sayfa duzeni (PAGE) icinde
    assert len(app.Db.requests) == 8


@pytest.mark.parametrize(
    "veri",
    [
        {"employee_id": "3", "start": "abc", "end": "2026-12-13"},
        {"employee_id": "3", "start": "", "end": "2026-12-13"},
        {"employee_id": "3", "start": "2026-12-13", "end": ""},
        {"employee_id": "3", "end": "2026-12-13"},  # baslangic hic gonderilmemis
        {"employee_id": "3", "start": "2026-02-30", "end": "2026-03-01"},  # takvimde yok
    ],
    ids=["cozulemez", "bos-baslangic", "bos-bitis", "gonderilmemis", "takvimde-yok"],
)
def test_repro_gecersiz_tarih_gorunur_hata(client, veri):
    """CB-1: gecersiz tarih -> 400, sayfa duzeni icinde tarih mesaji, kayit yok, yonlendirme yok."""
    _beklenen_tarih_hatasi(_gonder(client, veri))


def test_repro_bitis_baslangictan_once(client):
    """CB-2: bitis < baslangic -> kayit yok (eskiden -4 gunle kaydediliyordu), tarih mesaji."""
    _beklenen_tarih_hatasi(_gonder(client, {"employee_id": "3", "start": "2026-12-10", "end": "2026-12-05"}))


def test_tarih_dogrulamasi_bakiye_kontrolunden_once(client):
    """CB-2: kalani 0 olan Elif, bitis < baslangic -> ERR-412 degil, tarih mesaji."""
    yanit = _gonder(client, {"employee_id": "5", "start": "2026-12-10", "end": "2026-12-05"})
    _beklenen_tarih_hatasi(yanit)
    assert "ERR-412" not in yanit.get_data(as_text=True)


def test_tek_gunluk_talep_gecerli(client):
    """PB-1 siniri: baslangic = bitis gecerlidir, 1 gun sayilir, kaydedilir."""
    yanit = _gonder(client, {"employee_id": "3", "start": "2026-12-01", "end": "2026-12-01", "reason": "tek gun"})
    assert yanit.status_code == 302
    assert len(app.Db.requests) == 9
    yeni = app.Db.requests[-1]
    assert app.svc.calc_days(yeni["start"], yeni["end"]) == 1
