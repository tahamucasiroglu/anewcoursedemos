# Spec 0001 — walking-skeleton

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0001-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi — tahamucasiroglu "önerilerin uygun")

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
   tek sağ üst kontrol tema düğmesidir; diğerleri görünür ama "yakında" ipucuyla pasiftir. İkon henüz
   olmadığından logo yerinde, favicon'da ve tray'de yer tutucu ikon kullanılır; gerçek ikon `source/`a
   gelince aynı yerlere konur (trivial değişiklik).
2. Tema düğmesi açık ve koyu mod arasında geçiş yapar; seçim tarayıcıda hatırlanır. Varsayılan açık mod.
3. Her sayfa bağlantısı kendi sayfasını açar; sayfalar boş durumlarını gösterir. Yayınlama sayfası
   "Çok yakında sizlerleyiz" yazar.
4. Ekranın en altında ince "Günlük" şeridi vardır; bu spec'te son sistem olayını (ör. ajan bağlandı)
   gösterir. Geçmiş liste ve filtre 0021'de gelir.
5. Site, ürün versiyonunu (`0.1.0`) ve sistem durumunu (API erişilebilir mi, veritabanı erişilebilir mi,
   ajan bağlı mı) API'den alıp gösterir; durum 3 sn'de bir yenilenir.
6. Masaüstü ajanı başlatılınca görev çubuğunun sağ altında (tray) durur. Sağ tık menüsünde "Siteye git"
   (siteyi varsayılan tarayıcıda açar) ve "Çıkış" vardır. Pencere kapatmak uygulamayı kapatmaz.
7. Ajan çalışınca API'ye kendiliğinden bağlanır; bağlantı koparsa kendiliğinden yeniden bağlanır.
8. Sitede "Ajanı test et" eylemi, API üzerinden ajana bir komut gönderir ve ajanın cevabını (ajan
   versiyonu, makine adı) ekranda gösterir — API → ajan komut yolunun kanıtı.
9. Ajan bağlantı olayları veritabanındaki genel olay kaydına yazılır. Her olay tür, zaman (UTC), seviye
   (bilgi/uyarı/hata), metin ve isteğe bağlı kullanıcı alanlarını ayrı ayrı taşır; bu spec'te kullanıcı
   alanı boştur (sistem olayı). Sonraki spec'ler (giriş/çıkış, eşleştirme, üretim olayları) aynı kayda
   kullanıcıya bağlı olarak yazar; Günlük ve admin aktiviteleri (metni göstermeden) bu kayıttan okur. API
   ayağa kalkarken veritabanı şemasını kendisi hazırlar.
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
  - Pasif kontroller: tıklanınca bir şey olmaz; fareyle üzerine gelince ve klavye odağında "Yakında" ipucu.
  - Tema: açık · koyu.
- Design reference: `source/konseptler sayfası tasarımı.png`, `source/konseptten video.png`,
  `source/üretilen videolar.png` — commit `61cc824` sürümü (bu spec yalnızca üst bar ve sayfa iskeletini
  bu görsellerden alır; sayfa içerikleri ilgili spec'lerde). Renk: turuncu/kahve, açık mod esas (ilk_promt.txt).
- Accessibility: tüm bağlantılar ve tema düğmesi klavyeyle erişilebilir (Tab, Enter/Space); ikon
  düğmelerinin erişilebilir adı var; metin/arka plan kontrastı her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: yok. (Giriş/kayıt bu spec'te yok; skeleton uçları yerel ağda kimliksiz — 0002 ekler
  ve bu uçlar o zaman giriş ister.)
- Out of scope: kimlik, profil, model/API key, konsept, üretim, video oynatma, kurulum ekranı ve "Ajanı
  indir" bağlantısı (0009; bu spec'te ajan geliştirici build'inden çalışır), admin, hata bildirme akışı,
  loglama ayrıntıları (Serilog temel kurulumu hariç; maskeleme 0008), HTTPS (B-3).
- Pi: Web ve Api ayrı servisler; mevcut uygulamalarla (muskitakip, tmautoclip-canli, tahakonsept —
  tahakonsept ayrı bir projedir) isim, klasör ve port çakışması olmaz. Bu projenin Pi'deki adı
  **anewconcept**: klasör `/opt/anewconcept`, servisler `anewconcept-web` ve `anewconcept-api` (D3).
- Portlar: Web **8000**, Api **8001** (D2; 7100–7105 ve 5090 dolu, 8000'ler boş — 2026-11-14 kontrolü).
- Veritabanı: Pi'de kurulu **PostgreSQL 17**; ayrı `anewconcept` veritabanı ve kullanıcısı (D1).
- Docker: şimdilik yok (D4). Full profildeki entegrasyon testlerinin Docker'sız nasıl çalışacağı plan'da
  önerilir; Docker kurulunca Testcontainers'a geçilir (B-1).
- Split: Bu spec bir "yol haritası" spec'idir; kalan istekler aşağıdaki inşa sırasıyla ayrı spec'lerde
  gelir (Karar 1–2, 2026-11-14; bölünen parçalar yeni numara alır, harf eki yok). Bir spec yalnızca
  kendinden önce gelenlere dayanır:
  1. 0001 Walking skeleton
  2. 0008 Teknik loglama, saklama ve maskeleme (H-2)
  3. 0002 Kayıt, giriş, çıkış ve erişim (H-1) → 0011 Roller, yetkiler ve admin iskeleti (H-1) →
     0012 Ajan cihaz eşleştirme ve ajan sahipliği (H-1)
  4. 0003 Profiller (H-1 sahiplik)
  5. 0004 Katalog, API key yönetimi ve Ayarlar temeli (H-2) → 0013 Çağrı kapısı, harcama kaydı ve aylık gösterge (H-3)
  6. 0021 Günlük şeridi, geçmiş ve aktivite kaydı → 0022 Hata bildirimi (web, ajan, API)
  7. 0005 Model bilgisi ve ince ayar → 0014 Konsept üretimi ve onay (H-3) → 0015 Konsept düzenleme döngüsü →
     0016 Silinenler sayfası ve önerileri sıfırla
  8. 0009 Ajan ilk kurulum ve "Ajanı indir"
  9. 0006 Üretim ayarları ve sıra yönetimi → 0017 Üretim hattı (H-3) → 0018 Ajanda kısıtlı render (H-4)
  10. 0007 Üretilen videolar → 0019 Video silme ve Silinenler video sekmesi → 0020 Yeni sürüm ve yeniden üret (H-3)
  11. 0023 Ajan güncelleme ve kaldırma
  12. 0010 Admin: istatistikler ve aktiviteler → 0024 Admin: kullanıcılar ve ban (H-1) → 0025 Admin: hata bildirimi yönetimi

## Acceptance criteria
- [ ] AC-1 — Ana sayfa açıldığında üst bar Requirements 1'deki tüm öğeleri tasarımdaki sırayla gösterir; sayfa bağlantılarının her biri kendi sayfasını açar ve o sayfa boş durum metnini gösterir; Yayınlama "Çok yakında sizlerleyiz" gösterir.
- [ ] AC-2 — Tema düğmesi açık↔koyu geçiş yapar; sayfa yenilendiğinde son seçim korunur; hiç seçim yoksa açık mod gelir; renkler yalnızca tema değişkenlerinden gelir (K-7).
- [ ] AC-3 — Pasif kontroller (profil seçimi, kalem, artı, hata bildir, ayarlar) tıklanınca hiçbir eylem yapmaz; fareyle üzerine gelince ve klavye odağında "Yakında" ipucu görünür; tüm kontroller klavyeyle erişilebilir ve erişilebilir ada sahiptir.
- [ ] AC-4 — Durum alanı tek ürün versiyonunu (bu spec'te `0.1.0`; sabit metin değil, ürünün versiyon kaynağından okunur) ve API/DB/ajan durumlarını gösterir; API kapalıyken "API erişilemiyor", DB kapalıyken "DB erişilemiyor" hata durumu görünür; tüm istekler Web controller'ı üzerinden `$.ajax` ile gider (`fetch` yok, K-6).
- [ ] AC-5 — Ajan başlatılınca tray'de durur; "Siteye git" siteyi varsayılan tarayıcıda açar; "Çıkış" uygulamayı kapatır; pencere kapatmak uygulamayı kapatmaz.
- [ ] AC-6 — Ajan çalışırken site en geç 10 sn içinde "Ajan bağlı" gösterir; ajan kapanınca en geç 10 sn içinde "Ajan bağlı değil"e döner; API yeniden başlatılınca ajan kendiliğinden yeniden bağlanır.
- [ ] AC-7 — "Ajanı test et": ajan bağlıyken ajanın versiyonu ve makine adı ekranda görünür; ajan bağlı değilken "Ajan bağlı değil", cevap 10 sn'de gelmezse "Zaman aşımı" hatası görünür.
- [ ] AC-8 — Ajan bağlanma/kopma olayları veritabanına kaydedilir ve Günlük şeridinde son olay görünür; boş veritabanıyla başlayan API şemayı kendisi oluşturur.
- [ ] AC-9 — `scripts/check --fast` ve `scripts/check` (full) yeşil; residue testi her iki profilde temiz; mimari testleri Y-1…Y-6, K-1, K-2 ve K-4 kurallarının ihlalinde kırmızıya döner.
- [ ] AC-10 — Web ve Api Pi'de servis olarak çalışır, yerel ağdaki başka bir cihazdan site açılır; Pi yeniden başlatıldıktan sonra servisler kendiliğinden açılır; mevcut Pi uygulamaları etkilenmez (kanıt: kurulum öncesi ve sonrası diğer servislerin `systemctl is-active` çıktısı ve dinlenen port listesi aynı).

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
