# Karakterizasyon testleri ortak duzenegi (spec 0001).
# - "Bugun" referans tarihe (2026-11-21) sabitlenir: app.py "bugun"u yalniz datetime.now() ile okur
#   (docs/conventions.md, Known traps).
# - Her testten once seed() cagrilir: Db modul seviyesinde global, state testler arasinda sizmasin.
import datetime as _dt
import os
import sys

import pytest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import app  # noqa: E402

REFERANS_TARIH = _dt.date(2026, 11, 21)


class _SabitSaat(_dt.datetime):
    """datetime.datetime; yalniz now() referans tarihi dondurur (strptime vb. aynen calisir)."""

    @classmethod
    def now(cls, tz=None):
        return cls(REFERANS_TARIH.year, REFERANS_TARIH.month, REFERANS_TARIH.day, 10, 0, 0)


@pytest.fixture(autouse=True)
def sabit_saat_ve_seed(monkeypatch):
    monkeypatch.setattr(app, "datetime", _SabitSaat)
    app.seed()
    yield
    app.seed()


@pytest.fixture
def client():
    return app.app.test_client()
