# Spec 0001 — walking-skeleton

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0001-plan.md`
- Self-critique: pending (spec onayıyla birlikte; aşağıdaki Open questions)

## Intent
ANEW Konsept'in üç uygulaması (Web, Api, Agent) ve veritabanı ilk kez uçtan uca birbirine bağlanıyor.
Geliştirici (tek kullanıcı, yerel ağ) tarayıcıda turuncu/kahve temalı, açık/koyu modlu sitenin üst
barını ve tüm sayfalarını (boş halleriyle) gezebiliyor; sitenin API üzerinden veritabanına ve PC'deki
ajana ulaştığını ekranda görüyor. Başarı: sonraki her özellik bu iskeletin üzerine yalnızca kendi
modülünü ekleyerek gelebiliyor; `scripts/check` gerçek adımlarla (fast + full) yeşil ve iz bırakmıyor;
Web ve Api Pi'de çalışıyor. Bilinçli olarak yapılmayanlar: giriş/kayıt, profil, model, konsept üretimi,
video üretimi, kurulum ekranı, admin paneli — hepsi aşağıdaki yol haritasındaki ayrı spec'lerde.

## Requirements
1. Site açıldığında tasarımdaki üst bar görünür: solda logo yeri ve "ANEW Konsept" adı, yanında aylık
   harcama alanı; ortada sayfalar (Konseptler · Konseptten Video · Üretilenler · Silinenler · Yayınlama);
   sağda profil seçimi, kalem, artı, hata bildir, ayarlar ve güneş/ay tema düğmesi. Bu spec'te çalışan
   tek sağ üst kontrol tema düğmesidir; diğerleri görünür ama "yakında" ipucuyla pasiftir.
2. Tema düğmesi açık ve koyu mod arasında geçiş yapar; seçim tarayıcıda hatırlanır. Varsayılan açık mod.
3. Her sayfa bağlantısı kendi sayfasını açar; sayfalar boş durumlarını gösterir. Yayınlama sayfası
   "Çok yakında sizlerleyiz" yazar.
4. Ekranın en altında ince "Günlük" şeridi vardır; bu spec'te son sistem olayını (ör. ajan bağlandı) gösterir.
5. Site, ürün versiyonunu (`0.1.0`) ve sistem durumunu (API erişilebilir mi, veritabanı erişilebilir mi,
   ajan bağlı mı) API'den alıp gösterir; durum birkaç saniyede bir yenilenir.
6. Masaüstü ajanı başlatılınca görev çubuğunun sağ altında (tray) durur. Sağ tık menüsünde "Siteye git"
   (siteyi varsayılan tarayıcıda açar) ve "Çıkış" vardır. Pencere kapatmak uygulamayı kapatmaz.
7. Ajan çalışınca API'ye kendiliğinden bağlanır; bağlantı koparsa kendiliğinden yeniden bağlanır.
8. Sitede "Ajanı test et" eylemi, API üzerinden ajana bir komut gönderir ve ajanın cevabını (ajan
   versiyonu, makine adı) ekranda gösterir — API → ajan komut yolunun kanıtı.
9. Ajan bağlantı olayları veritabanına kaydedilir; API ayağa kalkarken veritabanı şemasını kendisi hazırlar.
10. Web ve Api, Pi'de servis olarak çalışır ve yerel ağdan erişilir; Pi yeniden başlasa da açılır.
11. `scripts/check` gerçek adımlarla çalışır: fast (derleme, format, birim + mimari testleri, `fetch(`/
    `style="` taramaları, JS testleri, bağımlılık açık taraması) ve full (+ PostgreSQL entegrasyon testleri).

## User experience
- Flow: Geliştirici tarayıcıda siteyi açar → üst bar ve Konseptler sayfası (boş) görünür → sayfalar
  arasında gezer → tema düğmesiyle koyu moda geçer → alt şeritte/durum alanında "API ✓ · DB ✓ · Ajan ✗"
  görür → PC'de ajanı başlatır → birkaç saniye içinde "Ajan ✓" olur → "Ajanı test et" der → ajanın
  makine adı ve versiyonu görünür. Ajan tray menüsünden "Siteye git" ile aynı siteyi açabilir.
- States:
  - Sayfa boş durumu: her sayfada "Henüz bir şey yok" türünde boş durum metni.
  - Yayınlama: "Çok yakında sizlerleyiz".
  - Durum alanı: yükleniyor · API erişilemiyor (hata) · DB erişilemiyor (hata) · ajan bağlı değil · ajan bağlı.
  - Ajanı test et: gönderiliyor · başarılı (cevap) · ajan bağlı değil (hata) · zaman aşımı (hata).
  - Pasif kontroller: tıklanınca bir şey olmaz, üzerine gelince "Yakında" ipucu.
  - Tema: açık · koyu.
- Design reference: `source/konseptler sayfası tasarımı.png`, `source/konseptten video.png`,
  `source/üretilen videolar.png` — commit `61cc824` sürümü (bu spec yalnızca üst bar ve sayfa iskeletini
  bu görsellerden alır; sayfa içerikleri ilgili spec'lerde). Renk: turuncu/kahve, açık mod esas (ilk_promt.txt).
- Accessibility: tüm bağlantılar ve tema düğmesi klavyeyle erişilebilir (Tab, Enter/Space); ikon
  düğmelerinin erişilebilir adı var; metin/arka plan kontrastı her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: yok. (Giriş/kayıt bu spec'te yok; skeleton uçları yerel ağda kimliksiz — 0002 ekler.)
- Out of scope: kimlik, profil, model/API key, konsept, üretim, video oynatma, kurulum ekranı, admin,
  hata bildirme akışı, loglama ayrıntıları (Serilog temel kurulumu hariç), HTTPS (B-3).
- Pi: Web ve Api ayrı servisler; mevcut uygulamalarla (muskitakip, tmautoclip-canli, tahakonsept —
  tahakonsept ayrı bir projedir) isim, klasör ve port çakışması olmaz. Bu projenin Pi'deki adı
  **anewconcept**: klasör `/opt/anewconcept`, servisler `anewconcept-web` ve `anewconcept-api` (D3).
- Portlar: Web **8000**, Api **8001** (D2; 7100–7105 ve 5090 dolu, 8000'ler boş — 2026-11-14 kontrolü).
- Veritabanı: Pi'de kurulu **PostgreSQL 17**; ayrı `anewconcept` veritabanı ve kullanıcısı (D1).
- Docker: şimdilik yok (D4). Full profildeki entegrasyon testlerinin Docker'sız nasıl çalışacağı plan'da
  önerilir; Docker kurulunca Testcontainers'a geçilir (B-1).
- Split: Bu spec bir "yol haritası" spec'idir; kalan istekler sırayla ayrı spec'lerde gelir:
  0002 Kimlik (kayıt, giriş 3 hak, çıkış, ajan eşleştirme — H-1) · 0003 Profiller (modal, şablonlar,
  üst bar seçimi) · 0004 AiGateway (sağlayıcı/model kataloğu, API key — H-2, harcama, ayarlar) ·
  0005 Konseptler (konudan/fikirden üret, düzenleme döngüsü, onay, Silinenler — H-3) · 0006 Üretim
  (kuyruk, aşamalar, ajan render — H-4; B-5 raporundan sonra) · 0007 Üretilen videolar (oynatıcı,
  yeni sürüm) · 0008 Günlük + hata bildirimi + loglama · 0009 Ajan kurulum/güncelleme ekranı ·
  0010 Admin paneli.

## Acceptance criteria
- [ ] AC-1 — Ana sayfa açıldığında üst bar Requirements 1'deki tüm öğeleri tasarımdaki sırayla gösterir; sayfa bağlantılarının her biri kendi sayfasını açar ve o sayfa boş durum metnini gösterir; Yayınlama "Çok yakında sizlerleyiz" gösterir.
- [ ] AC-2 — Tema düğmesi açık↔koyu geçiş yapar; sayfa yenilendiğinde son seçim korunur; hiç seçim yoksa açık mod gelir; renkler yalnızca tema değişkenlerinden gelir (K-7).
- [ ] AC-3 — Pasif kontroller (profil seçimi, kalem, artı, hata bildir, ayarlar) tıklanınca hiçbir eylem yapmaz ve "Yakında" ipucu verir; tüm kontroller klavyeyle erişilebilir ve erişilebilir ada sahiptir.
- [ ] AC-4 — Durum alanı versiyonu `0.1.0` olarak ve API/DB/ajan durumlarını gösterir; API kapalıyken "API erişilemiyor", DB kapalıyken "DB erişilemiyor" hata durumu görünür; tüm istekler Web controller'ı üzerinden `$.ajax` ile gider (`fetch` yok, K-6).
- [ ] AC-5 — Ajan başlatılınca tray'de durur; "Siteye git" siteyi varsayılan tarayıcıda açar; "Çıkış" uygulamayı kapatır; pencere kapatmak uygulamayı kapatmaz.
- [ ] AC-6 — Ajan çalışırken site en geç 10 sn içinde "Ajan bağlı" gösterir; ajan kapanınca en geç 10 sn içinde "Ajan bağlı değil"e döner; API yeniden başlatılınca ajan kendiliğinden yeniden bağlanır.
- [ ] AC-7 — "Ajanı test et": ajan bağlıyken ajanın versiyonu ve makine adı ekranda görünür; ajan bağlı değilken "Ajan bağlı değil", cevap 10 sn'de gelmezse "Zaman aşımı" hatası görünür.
- [ ] AC-8 — Ajan bağlanma/kopma olayları veritabanına kaydedilir ve Günlük şeridinde son olay görünür; boş veritabanıyla başlayan API şemayı kendisi oluşturur.
- [ ] AC-9 — `scripts/check --fast` ve `scripts/check` (full) yeşil; residue testi her iki profilde temiz; mimari testleri Y-1…Y-6, K-1, K-2 ve K-4 kurallarının ihlalinde kırmızıya döner.
- [ ] AC-10 — Web ve Api Pi'de servis olarak çalışır, yerel ağdaki başka bir cihazdan site açılır; Pi yeniden başlatıldıktan sonra servisler kendiliğinden açılır; mevcut Pi uygulamaları etkilenmez.

## Open questions (awaiting human)
Bu bölüm spec onaylanınca kaldırılır; kararlar yukarıya işlenir.
- Kararlar (2026-11-14, alındı): D1 PostgreSQL 17 · D2 portlar 8000'ler → Web 8000, Api 8001 (öneri, onayda teyit) ·
  D3 Pi'de ad `anewconcept` · D4 şimdilik Docker'sız · D5 giriş/kayıt 0002'de (öneri, onayda teyit).
- Self-critique boşlukları (önerilerimle, onayda katlanır):
  1. AC-6/AC-7 "10 sn": durum yoklaması 3 sn aralıkla; 10 sn sınırı ölçülebilir kalsın — öneri: kalsın.
  2. AC-3 "Yakında" ipucu: fareyle üzerine gelince ve klavye odağında görünen ipucu — öneri: böyle.
  3. AC-8 Günlük şeridi bu spec'te yalnızca son olayı gösterir; geçmiş liste 0008'de — öneri: böyle.
  4. AC-10 "mevcut Pi uygulamaları etkilenmez" kanıtı: kurulum öncesi/sonrası diğer servislerin
     `systemctl is-active` ve port listesi karşılaştırması — öneri: böyle.

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise / deferred | |
| Regressions introduced | |
| Bugs escaped to production | |
