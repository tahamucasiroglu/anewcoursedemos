# 0003 — Dil onayı · Adım 1 İNCELEME · Görüşme konusu 1: Ürün ve hedef

**Tarih:** 2026-11-19 14:30

## İstek

> zaten dökümanda olsada sırayla süreç ilerlesin tekrar göreyim bende.  chat=tr  docs=tr

## Cevap

**Adım 0 — Dil: ONAYLANDI** → görüşme dili `tr`, doküman dili `tr`. Bu ayar ÜRETİM adımında AGENTS.md `Language:` satırına ve `docs/conventions.md` dosyasına yazılacak.
Sıra kuralı: Doküman bir konuyu zaten cevaplıyor olsa bile her konu sırayla ayrı ayrı sorulacak.

**Adım 1 — İNCELEME:** Çalışma ağacında uygulama kodu yok. Yalnızca ANEW dosyaları, `okul-yonetim-platformu-kapsam.md` ve `Promtlar/` var. Sonuç: **sıfırdan proje (greenfield) akışı**. Cevaplar senden gelecek; mevcut koddan çıkarılacak bir şey yok.

**Adım 2 — GÖRÜŞME · Konu 1/12: Ürün ve hedef**

Önerilen ürün tanımı (`docs/domain.md` + AGENTS.md proje özeti):
- **Ürün:** Okul Yönetim Platformu. Küçük ve orta ölçekli özel okullar ile kurs/etüt merkezleri için AI-native, çok kiracılı (multi-tenant) bir SaaS okul işletim sistemi.
- **Hedef:** Veli iletişimini ve tahsilatı tek uygulamada toplamak, öğretmene günde 1 saat kazandırmak.
- **Bu reponun odağı:** Faz 1 (MVP). Faz 2 ve Faz 3 yol haritasında kayıtlı kalır, ama kapılar (Kapı 1/2) geçilmeden bunlar için spec yazılmaz.

Önerim: Tanımı dokümandaki gibi almak, **odağı Faz 1 ile sınırlamak**. Gerekçe: Doküman, "her aşama önündeki kapının kriteri sağlanmadan başlamaz" diyor. Bu bir ödev/kurs reposu; dar bir MVP, ANEW döngüsünü (spec → plan → build → review → verify) gerçek işle göstermeye yeter.

**Dokümanda istenen:**
- Ürün: KOBİ ölçeğindeki özel okullar ve kurs/etüt merkezleri için AI-native bir okul işletim sistemi.
- Satış kancası velinin mobil deneyimi; satışı kapatan değer ise kurucunun derdini (tahsilat, kayıt, iletişim yükü) çözmek.
- Hedef segment: Tek şubeli veya 2–5 şubeli özel okul, kolej, kurs/etüt merkezi. Karar verici: kurucu veya genel müdür.
- Hedef dışı: İlk 18 ay büyük zincir kolejler (K12NET'in alanı) ve devlet okulları.
- Değer önerisi: "Veliyle iletişimi ve tahsilatı tek uygulamada toplayın, öğretmenin günde 1 saatini geri kazanın."
- Farklılaşma: Modern mobil deneyim, AI ile otomatik veli iletişimi, Excel/e-Okul verisinden hızlı geçiş.
- Yol haritası: Faz 1 MVP → Kapı 1 (pilotlar bir dönemi tamamladı) → Faz 2 → Kapı 2 (ilk ücretli yenilemeler) → Faz 3.

Cevap bekleniyor: Ürün tanımı bu şekilde ve odak Faz 1 olsun mu? Eklemek ya da değiştirmek istediğin bir şey var mı?
