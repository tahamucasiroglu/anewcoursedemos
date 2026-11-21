# Spec 0001 — kidem -> hak karakterizasyonu (AC-4). Referans tarih 2026-11-21 (conftest).
import datetime as dt

import pytest

import app


def _hak(ise_giris):
    return app.svc.get_entitlement({"id": 99, "name": "Test", "department": "IT", "hire_date": ise_giris})


@pytest.mark.parametrize(
    "kidem, beklenen_hak",
    [(0, 0), (1, 14), (5, 14), (6, 20), (15, 20), (16, 26)],
)
def test_kidem_bantlari_sinirlar(kidem, beklenen_hak):
    """AC-4 / BR-1: <1 -> 0 · 1–5 -> 14 (yorum '15' der, kod 14) · 6–15 -> 20 · 16+ -> 26."""
    assert _hak(dt.date(2026 - kidem, 6, 1)) == beklenen_hak


def test_kidem_yalniz_yil_farki():
    """AC-4 / BR-2: kidem = yil farki, ay/gun yok sayilir. 2025-12-31 giris -> kidem 1 -> 14 gun."""
    assert app.svc.get_seniority_years({"hire_date": dt.date(2025, 12, 31)}) == 1
    assert _hak(dt.date(2025, 12, 31)) == 14
