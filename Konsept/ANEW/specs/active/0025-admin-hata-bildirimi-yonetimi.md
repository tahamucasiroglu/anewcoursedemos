# Spec 0025 — Admin: hata bildirimi yönetimi

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0025-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0010-admin-paneli.md` (eski 0010c; bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun")
- Sensitive area: H-1 Kimlik ve yetki (bildirim verisinin yalnızca `admin.panel` ile okunup değiştirilmesi); H-2 Gizli bilgiler (bildirim detayında API key'in yalnızca maskeli görünmesi; maskelemenin kendisi 0008'de)
- Depends on: 0008, 0011, 0022, 0010

## Intent
Admin, kullanıcıların web'den ve ajandan bildirdiği hatalarla API'nin otomatik topladığı beklenmeyen
hataları panelde incelemek ve yönetmek istiyor (ilk_promt.txt: "API'ye giden hatalar depolanmalı ve bir
adet admin panel gerekecek bunlara bakmak ve yönetmek için"; `Promtlar/0004`: "masaüstü ile web'den
bildirilen hataların incelenmesi"). Başarı: admin açık bildirimleri görüp detayını okuyabiliyor ve
durumunu değiştirebiliyor; açık sayısı doğru kalıyor; detayda hiçbir gizli bilgi maskesiz görünmüyor.
Bilinçli olarak yapılmayanlar: bildirim silme, kullanıcının kendi bildirimlerinin durumunu takip etmesi,
admin'e bildirim (e-posta vb.), teknik log görüntüleyici.

## Requirements
1. Panelin **Hata bildirimleri** sekmesi (sekme sırasında son; panel iskeleti 0010) 0022'de toplanan
   bildirimleri sayfa başına 25 kayıtla, en yeni üstte listeler: zaman, kaynak (web / ajan / API — Ç-6),
   bildiren kullanıcı (API kaynaklı otomatik kayıtta kullanıcı varsa o, yoksa "—"), kısa mesaj, durum.
2. Durumlar: **Yeni → İnceleniyor → Çözüldü | Yok sayıldı**. "Açık" = Yeni + İnceleniyor. 0022 her
   bildirimi "Yeni" olarak saklar. Bildirim silinmez; istenmeyen bildirim "Yok sayıldı" ile iz bırakır.
3. Liste kaynağa (Tümü / web / ajan / API) ve duruma (Açık / Yeni / İnceleniyor / Çözüldü / Yok sayıldı /
   Tümü) göre filtrelenebilir; varsayılan görünüm açık bildirimlerdir.
4. Admin bir bildirimin detayını açar: kullanıcının yazdığı açıklama, teknik bilgi (ajan/web'in gönderdiği
   ya da API'nin topladığı), korelasyon numarası, ajan/uygulama versiyonu. Admin son kullanıcı olmadığından
   teknik bilgi gösterilir; ancak şifre ve token hiç, API key yalnızca maskeli (`sk-…a3f`) görünür
   (`docs/security.md`, maskeleme 0008). Tüm içerik HTML olarak yorumlanmadan düz metin gösterilir
   (bildirimdeki `<script>` gibi metin çalışmaz, olduğu gibi görünür).
5. Admin detaydan bildirimin durumunu dört durumdan herhangi birine getirebilir ve isteğe bağlı bir admin
   notu ekleyebilir (en fazla 500 karakter). Geçersiz bir durum değeri ya da var olmayan bir bildirim
   reddedilir. Durum değişince liste ve 0010'un İstatistikler bölümündeki açık hata bildirimi sayısı
   güncellenir.
6. Admin bildirim içeriğinde kullanıcının konsept, profil ya da API key içeriğine başka bir yoldan ulaşamaz
   (BR-1); bildirim kullanıcının bilinçli gönderdiği veridir.

## User experience
- Flow: Admin paneli açar → Hata bildirimleri sekmesine geçer → açık bildirimler listelenir → kaynağa ya da
  duruma göre filtreler → bir bildirimi açar, detayı okur → durumunu "İnceleniyor"/"Çözüldü"/"Yok sayıldı"
  yapar, isteğe bağlı not yazar → liste ve açık sayısı güncellenir.
- States:
  - Liste: yükleniyor · liste · yükleme hatası (Türkçe sade mesaj + korelasyon numarası + "Tekrar dene") ·
    boş ("Açık hata bildirimi yok") · filtre sonucu boş ("Filtreye uyan bildirim yok").
  - Detay: yükleniyor · detay · yükleme hatası · var olmayan bildirim (hata mesajı).
  - Durum değiştirme: işleniyor (eylem pasif) · başarılı · başarısız (durum değişmez, hata mesajı) · not
    sınırı aşıldı ("En fazla 500 karakter", kaydet pasif).
- Design reference: none — the flow above is the design. (Admin paneli tasarım görsellerinde yok; panel
  iskeleti 0010'dan.)
- Accessibility: tablo, filtreler, sayfalama, detay ve durum eylemleri klavyeyle kullanılabilir (Tab,
  Enter/Space, Esc detay/pencereyi kapatır, odak geri döner); durum yalnızca renkle değil metinle de
  gösterilir; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-1 Kimlik ve yetki; H-2 Gizli bilgiler. Lite modda da netleştirme sorulur, her
  düzeltme turu yeniden review edilir ve AC-1 ile AC-5 kasıtlı bozma ile kanıtlanır (yetki kontrolü ya da
  maskeleme kapatılınca test kırmızı).
- Bildirimlerin toplanması, kaynakları (web / ajan / API otomatik), saklama ve normal kullanıcının başka
  kullanıcıların bildirimlerini okuyamaması 0022'dedir; maskeleme mekanizması 0008'dedir. Bu spec yalnızca
  admin tarafında listeleme, detay ve durum yönetimini ekler.
- Veri kaynağı sınırı: veri Monitoring'in açık sözleşmesinden okunur (`docs/architecture.md`).
- Panel uçlarına rate limit uygulanmaz (yalnızca admin, LAN).
- Out of scope: bildirim silme · kullanıcının kendi bildirimlerinin durumunu takip etmesi · kullanıcıya
  durum bildirimi · admin'e bildirim (e-posta vb.) · teknik log (Serilog) görüntüleyici · dışa aktarma
  (CSV) · canlı yenileme.
- Split: done — 0025 Hata bildirimi yönetimi / kardeşler: 0010, 0024

## Acceptance criteria
- [ ] AC-1 — Hata bildirimi listesi, detay ve durum değiştirme istekleri `admin.panel` yetkisi olmayan bir kullanıcının token'ıyla sunucuya doğrudan gönderildiğinde reddedilir; hiçbir veri dönmez ve hiçbir değişiklik olmaz; yetki kontrolü kaldırıldığında bu test kırmızıya döner.
- [ ] AC-2 — Hata bildirimleri bölümü varsayılan olarak açık (Yeni + İnceleniyor) bildirimleri en yeni üstte, sayfa başına 25 kayıtla zaman, kaynak, bildiren, kısa mesaj ve durumla listeler; sayfalama tüm kayıtlara erişim sağlar; kaynak (web / ajan / API) ve durum filtreleri bilinen veri setiyle doğru sonuç verir; açık bildirim yoksa "Açık hata bildirimi yok", filtreye uyan yoksa "Filtreye uyan bildirim yok" görünür.
- [ ] AC-3 — Liste ve detay yüklenirken yükleniyor durumunu, sunucu hatasında Türkçe sade mesaj + korelasyon numarası + "Tekrar dene"yi gösterir; panelin kendi hatalarında stack trace, SQL ya da gizli bilgi görünmez.
- [ ] AC-4 — Bir bildirimin detayı açıklama, teknik bilgi, korelasyon numarası ve versiyonu gösterir; içerikteki `<script>` gibi metin çalışmaz, düz metin olarak görünür.
- [ ] AC-5 — İçinde API key, şifre ve token geçen bir bildirimin detayında ve onu besleyen yanıtta API key yalnızca maskeli (`sk-…a3f`) görünür, şifre ve token hiç görünmez; maskeleme kasıtlı olarak kapatıldığında test kırmızıya döner (H-2).
- [ ] AC-6 — Admin bildirimin durumunu değiştirirken istek sürerken eylem pasiftir; başarılıysa liste ve İstatistikler'deki açık bildirim sayısı güncellenir ("Çözüldü" ya da "Yok sayıldı" yapılan bildirim 0010'daki açık sayıdan düşer, "İnceleniyor" açık sayılmaya devam eder, açık duruma geri alınan yeniden sayılır — 0010 AC-3'ün devrettiği kanıt) ve isteğe bağlı not (en fazla 500 karakter; aşınca kaydet pasif ve "En fazla 500 karakter") saklanıp detayda görünür; sunucu hatasında durum değişmez ve hata mesajı görünür; geçersiz bir durum değeri ya da var olmayan bir bildirim reddedilir ve durum değişmez; bildirimi silen bir eylem yoktur.
- [ ] AC-7 — Hata bildirimleri ekranında ve onu besleyen yanıtlarda, bildirimin kendi içeriği dışında hiçbir kullanıcının konsept başlığı/metni, prompt'u, profil içeriği ya da API key'i yer almaz (bilinen içerikli veri setinde arama ile kanıtlanır).
- [ ] AC-8 — Bu spec'teki tüm eylemler klavyeyle yapılabilir (Tab, Enter/Space, Esc detayı kapatır, odak geri döner); durumlar metinle de gösterilir; her iki temada kontrast WCAG AA; tarih/saatler yerel saatle (Europe/Istanbul) gösterilir.

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
