# Spec 0001 — yeni talep karakterizasyonu (AC-5…AC-9). Tuhafliklar OLDUGU GIBI sabitlenir.
import app

ERR_412 = "HATA: ERR-412 NullReferenceGibi - bakiye yetersiz. IT'ye bildirin: dahili 4130"  # spec 0003


def _gonder(client, personel, baslangic, bitis, neden="test"):
    return client.post(
        "/new",
        data={"employee_id": str(personel), "start": baslangic, "end": bitis, "reason": neden},
    )


def test_gecerli_talep_beklemede_dogar(client):
    """AC-5: kalan > gun -> kaydedilir, Beklemede, no 9, listeye yonlendirir ve listede gorunur."""
    yanit = _gonder(client, 2, "2026-12-07", "2026-12-08", "Bayram")
    assert yanit.status_code == 302
    assert yanit.headers["Location"].endswith("/requests")
    assert len(app.Db.requests) == 9
    yeni = app.Db.requests[-1]
    assert (yeni["id"], yeni["employee_id"], yeni["status"]) == (9, 2, "Beklemede")
    liste = client.get("/requests").get_data(as_text=True)
    assert "<td>Mehmet Demir</td><td>2026-12-07</td><td>2026-12-08</td><td>2</td><td>Bayram</td>" in liste


def test_form_esigi_err412_dahili_4127(client):
    """AC-6 / BR-7 form tarafi: kalan < gun -> ERR-412 sayfasi (yonlendirme yok), kayit yok.
    Supersedes 0001/AC-6 — spec 0003: dahili numara 4130; adindaki 4127 tarihsel."""
    yanit = _gonder(client, 5, "2026-12-01", "2026-12-01")  # Elif Sahin, kalan 0, 1 gun
    assert yanit.status_code == 200
    html = yanit.get_data(as_text=True)
    assert ERR_412 in html
    assert "4127" not in html
    assert len(app.Db.requests) == 8


def test_servis_esigi_kalan_esit_gun_sessizce_kaydedilmez(client):
    """AC-7 / BR-7 servis tarafi (Q-1, karar yok): kalan == gun -> hata yok, yonlendirme var,
    kayit YOK. Bir gun eksigi kaydedilir."""
    yanit = _gonder(client, 3, "2026-12-01", "2026-12-14")  # Zeynep Kaya, kalan 14, 14 gun
    assert yanit.status_code == 302
    assert yanit.headers["Location"].endswith("/requests")
    assert len(app.Db.requests) == 8

    yanit = _gonder(client, 3, "2026-12-01", "2026-12-13")  # 13 gun
    assert yanit.status_code == 302
    assert len(app.Db.requests) == 9


def test_gecersiz_tarih_sessizce_yonlendirir(client):
    """AC-8 (yutulan hata): cozulemeyen tarih -> hata yok, listeye yonlendirme, kayit yok."""
    yanit = _gonder(client, 3, "abc", "2026-12-13")
    assert yanit.status_code == 302
    assert yanit.headers["Location"].endswith("/requests")
    assert len(app.Db.requests) == 8


def test_gecmis_tarihli_talep_kabul(client):
    """AC-9 / BR-6: baslangici referans tarihten (2026-11-21) once olan talep kaydedilir."""
    yanit = _gonder(client, 3, "2026-01-05", "2026-01-06", "Geriye donuk")
    assert yanit.status_code == 302
    assert len(app.Db.requests) == 9
    assert app.Db.requests[-1]["start"].isoformat() == "2026-01-05"
