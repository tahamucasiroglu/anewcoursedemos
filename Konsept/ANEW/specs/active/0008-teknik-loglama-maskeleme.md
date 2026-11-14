# Spec 0008 — teknik-loglama-maskeleme

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0008-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: specs/active/0008-gunluk-hata-bildirimi-loglama.md — Req 7, 8, 13 + C-6 + self-critique 3, 7 (bölündü 2026-11-14, onay: tahamucasiroglu 'önerilerin uygun')
- Sensitive area: H-2 Gizli bilgiler (teknik loglarda, olay kaydında ve bundan sonra gelen her kayıtta maskeleme mekanizması ve kanıtı)
- Depends on: 0001

## Intent
Geliştirici/admin, sorunları yakalayabilmek için Web, Api ve ajanda seviyeli teknik loglara ihtiyaç duyuyor
(ilk_promt.txt: "loglama olacak. MVC'de olacak, API'de olacak, masaüstü programında da olacak";
"information, warning, error, critical"; "Lokal loglar bir hafta süreli tutulacak"). Bu loglar ve sistemin
tuttuğu her kayıt gizli bilgi sızdırmamalı: şifre hiç görünmez, API key yalnızca maskeli görünür. Başarı:
bir istekte kullanıcıya dönen korelasyon numarasıyla Api logunda ilgili kayıt bulunur; yerel loglar 7 gün
sonra kendiliğinden silinir; Api ve Web'in uyarı ve hataları 30 gün veritabanında aranabilir; maskeleme
mekanizması tek yerde kurulur ve sonraki spec'ler (0002, 0004, 0021, 0022 …) kendi alanlarını yalnızca
birer kriterle kanıtlar (Ç-9). Bilinçli olarak yapılmayanlar: Günlük şeridinin geçmişi ve filtreleri
(0021), hata bildirimi (0022), teknik logların arayüzde gösterilmesi, harici log/izleme servisleri.

## Requirements
1. **Teknik loglama:** Web, Api ve ajan teknik loglarını Information, Warning, Error, Critical
   seviyeleriyle yazar. Her kayıtta zaman, seviye, kaynak uygulama (Web / Api / ajan) ve (varsa) korelasyon
   numarası bulunur. Teknik log, kullanıcıya gösterilen Günlük'ten ayrıdır (domain.md: Günlük ≠ Teknik log).
2. **Korelasyon:** Kullanıcıya ProblemDetails ile dönen hata yanıtındaki korelasyon numarası Api logundaki
   ilgili kayıtta bulunur.
3. **Olaylar logda da:** 0001'in olay kaydına (Günlük'ün kaynağı) yazılan her olay teknik loga da yazılır
   (ilk_promt.txt: "Bunlar loglara da kaydedilecek"). Seviye eşlemesi: Günlük Bilgi → Information, Uyarı →
   Warning, Hata → Error, Kritik → Critical.
4. **Yerel log saklama:** Dosyaya yazılan teknik loglar (ajanın kullanıcı PC'sinde, Web ve Api'nin Pi'de)
   son 7 günü tutar; daha eski log dosyaları kendiliğinden silinir.
5. **Yerel log klasörü:** Ajan loglarını kendi klasörünün (ajanın çalıştığı/kurulduğu klasör) altındaki
   `logs` klasörüne yazar; Web ve Api Pi'de `/opt/anewconcept` altına yazar. Başka "Konsept" programlarıyla
   çakışan ortak bir klasör kullanılmaz (0009 kuralı). Dosya adları plan'da belirlenir.
6. **Veritabanında teknik log:** Api ve Web'in Warning, Error ve Critical logları ayrıca veritabanında 30 gün
   tutulur; daha eskileri kendiliğinden silinir. Information logları veritabanına yazılmaz.
7. **Ajan logunun API'ye akışı:** Ajan, API'ye bağlıyken yalnızca Error ve Critical log özetlerini (seviye,
   zaman, maskelenmiş mesaj, ajan versiyonu) mevcut ajan bağlantısı üzerinden API'ye gönderir; bunlar
   kaynak "ajan" ile veritabanındaki teknik loga yazılır (30 gün kuralı geçerli). Ajanın tam logu ajanda 7 gün
   kalır; API'ye yalnızca kullanıcı bir hata bildirimiyle gönderirse gider (0022). Yeni bir kimliksiz uç
   açılmaz.
8. **Gizli bilgi maskeleme (H-2):** Teknik loglarda (dosya ve veritabanı), ajan özetlerinde ve olay kaydında
   şifre hiçbir zaman yer almaz; API key yalnızca maskeli (`sk-…a3f`: önek + son 3 karakter) görünür;
   erişim/yenileme token'ları, cihaz eşleştirme kodu ve eşleştirme onay sırrı, video akış token'ı ve
   veritabanı şifresi düz metin olarak görünmez. Bu bilgiler bir kayda adlandırılmış bir alan olarak da,
   serbest metnin (hata mesajı, kullanıcı metni) içinde de girse maskelenir.
9. **Serbest metinde tanınan biçimler:** Bilinen API key biçimleri (`sk-`, `sk-ant-`, `sk-or-` vb.) ve JWT
   biçimi serbest metin içinde de tanınıp maskelenir. Maskelenen biçimlerin ve alan adlarının tam listesi
   plan'da test tablosu olarak sabitlenir; sonraki spec'ler yeni bir gizli alan eklerse bu tabloya ekler.
10. **Tek mekanizma:** Maskeleme tek bir ortak mekanizmadır; sonraki spec'lerin yazdığı her kayıt (Günlük,
    hata bildirimi, teknik log) bu mekanizmadan geçer. Sonraki spec'ler kendi alanlarının maskeli olduğunu
    birer kriterle kanıtlar (Ç-9).

## User experience
none — no user interface. Bu spec'in davranışı log dosyalarında, veritabanındaki teknik log kaydında ve
olay kaydında gözlenir. (Kullanıcıya dönen hata mesajındaki korelasyon numarası 0001'deki hata gösterimini
kullanır; yeni bir ekran eklenmez.)

## Constraints & out of scope
- Sensitive area: H-2 Gizli bilgiler (bkz. başlık). Maskelemeyi kaldıran ya da atlatan her değişiklik
  testleri kırmızıya çevirmelidir.
- Zamanlar UTC saklanır (log dosyası ve veritabanı).
- Pi'de disk/RAM tasarrufu: veritabanına yalnızca Warning+ (Api, Web) ve ajanın Error/Critical özetleri yazılır.
- Ajan bağlantısı: Req 7'deki "mevcut ajan bağlantısı", 0012'ye kadar 0001/0002'deki geçici kimliksiz bağlantıdır
  (LAN, tek kullanıcı; bu aralıkta Pi'ye sürüm çıkılmaz); 0012 bu bağlantıyı kimlikli yapar ve özetler oradan
  gitmeye devam eder. Bu spec yeni bir uç açmaz.
- Out of scope: Günlük şeridinin kişiye özel gösterimi, geçmişi, filtresi (0021); hata bildirimi akışları ve
  API'nin beklenmeyen hatalarının otomatik hata kaydı olarak toplanması (0022); teknik logların admin
  panelinde ya da başka bir arayüzde gösterilmesi (geliştirici veritabanından/dosyadan okur); ajanın API'ye
  bağlı değilken oluşan Error/Critical özetlerinin sonradan gönderilmesi (tam log ajanda 7 gün kalır);
  harici log/izleme servisleri.
- Split: done — 0008 Teknik loglama, saklama ve maskeleme / kardeşler: 0021, 0022

## Acceptance criteria
- [ ] AC-1 — Web, Api ve ajan Information/Warning/Error/Critical seviyelerinde teknik log yazar; her kayıtta zaman (UTC), seviye, kaynak uygulama (Web / Api / ajan) ve varsa korelasyon numarası bulunur.
- [ ] AC-2 — Bir istek ProblemDetails hata yanıtıyla döndüğünde yanıttaki korelasyon numarasıyla Api logunda ilgili kayıt bulunur.
- [ ] AC-3 — Olay kaydına yazılan bir olay (ör. 0001'in "Ajan bağlandı" olayı) teknik logda da bulunur; seviyesi eşlemeye uyar (Bilgi → Information, Uyarı → Warning, Hata → Error, Kritik → Critical).
- [ ] AC-4 — Yerel log dosyalarından 7 günlük olan korunur, 8 günlük olan kendiliğinden silinir (ajan, Web ve Api için ayrı ayrı).
- [ ] AC-5 — Ajan loglarını yalnızca kendi klasörünün altındaki `logs` klasörüne, Web ve Api yalnızca `/opt/anewconcept` altına yazar; bu klasörlerin dışında log dosyası oluşmaz.
- [ ] AC-6 — Api ve Web'in Warning, Error ve Critical logları veritabanına yazılır, Information logları yazılmaz; veritabanındaki 30 günlük teknik log kaydı korunur, 31 günlük olan kendiliğinden silinir.
- [ ] AC-7 — Ajan API'ye bağlıyken oluşan Error ve Critical logları maskelenmiş özet olarak (seviye, zaman, mesaj, ajan versiyonu) veritabanındaki teknik loga kaynak "ajan" ile düşer; ajanın Information ve Warning logları ve tam log dosyası API'ye gönderilmez; özet gönderimi için kimliksiz yeni bir uç yoktur.
- [ ] AC-8 — Şifre, maskesiz API key, erişim/yenileme token'ı, cihaz eşleştirme kodu, eşleştirme onay sırrı, video akış token'ı ve veritabanı şifresi; bunları adlandırılmış alan olarak içeren bir kayıt oluşturulduğunda log dosyasında, veritabanındaki teknik logda, ajan özetinde ve olay kaydında düz metin olarak görünmez; şifre hiçbir biçimde (maskeli dahil) yer almaz; API key `sk-…a3f` biçiminde görünür.
- [ ] AC-9 — Serbest metin (hata mesajı, kullanıcı metni) içine yerleştirilmiş `sk-`, `sk-ant-`, `sk-or-` önekli key'ler ve JWT biçimli token'lar plan'daki test tablosundaki her örnek için maskelenir; maskeleme kasıtlı olarak kaldırıldığında ya da atlatıldığında testler kırmızıya döner (H-2).

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
