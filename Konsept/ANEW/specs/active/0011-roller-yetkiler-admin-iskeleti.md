# Spec 0011 — Roller, yetkiler ve admin iskeleti

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0011-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0002-kimlik.md` (Req 4'ün rol kısmı, Req 16–18, AC-1'in rol kısmı, AC-14–15) — bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: H-1 Kimlik ve yetki
- Depends on: 0001, 0002

## Intent
ANEW Konsept'te kullanıcının neleri kullanabileceği rolüne bağlı yetkilerle belirlenir (ilk_promt.txt:
"login olması haricinde yetkileri olacak"; BR-13 temeli). Bu spec iki rolü (varsayılan **kullanıcı** ve
**admin**), yetki kararının her zaman sunucuda verildiği bir iskeleti, "yetkiniz yok" durumlarını ve
yalnızca admin'e görünen boş bir admin alanını kurar. Başarı: kullanıcı rolündeki biri admin alanına ve
admin verisine hiçbir yoldan ulaşamıyor; ilk admin yalnızca ortam ayarıyla belirleniyor ve kimse kendine
admin rolü veremiyor. Bilinçli olarak yapılmayanlar: admin panelinin içeriği (0010, 0024, 0025), model
bazlı yetkilerin uygulanması (0013), `publish.use` (yayınlama gelince), rol yönetim arayüzü.

## Requirements
1. Kullanıcının neleri kullanabileceği rolüne bağlı yetkilerle belirlenir. İki rol vardır: **kullanıcı**
   (varsayılan) ve **admin**. 0002 ile açılan her yeni hesap kullanıcı rolüyle açılır.
2. Admin rolü admin alanına giriş yetkisini (`admin.panel`) taşır; kullanıcı rolü taşımaz. Diğer yetkiler
   kendi spec'lerinde uygulanır: `model.use:<model>` ve kullanıcı rolünün hangi modelleri taşıyacağı
   0013'te (0013 Req 2; o güne kadar model kısıtı yok); `publish.use` yayınlama gelince.
3. Yetkisi olmayan bir sayfayı açan kullanıcı "Bu sayfaya erişim yetkiniz yok", yetkisi olmayan bir işlemi
   isteyen kullanıcı "Bu işlem için yetkiniz yok" durumuyla karşılaşır; sunucuya doğrudan gönderilen
   yetkisiz istek yetkisiz yanıt alır ve veri döndürmez. Arayüz yetkisiz bağlantıları gizler ama karar her
   zaman sunucudadır. (0010 AC-1 bu kritere dayanır; aynı test iki kez yazılmaz.)
4. Admin giriş noktası: üst barda, "Ayarlar"ın yanında, yalnızca `admin.panel` yetkisi olanlara görünen
   "Admin" ikonu. Admin alanı bu spec'te yalnızca bir iskelettir: boş durum metni gösteren tek bir sayfa;
   içeriğini 0010 (istatistikler, aktiviteler), 0024 (kullanıcılar, ban) ve 0025 (hata bildirimleri)
   doldurur.
5. İlk admin hesabı yalnızca Pi'deki ortam ayarında (gizli bilgi değil) verilen **bir e-posta adresiyle**
   belirlenir: o e-postayla kayıt olunduğunda hesap admin rolü alır; API açılışında o e-postaya ait hesap
   zaten varsa admin rolü alır. E-posta eşleşmesi 0002 Req 2'deki karşılaştırma kuralıyla yapılır
   (büyük/küçük harf ve kültür bağımsız). Ayar boşsa hiçbir hesap otomatik admin olmaz. Başka hiçbir yolla
   (ör. kayıt formuna ek alan göndermek, doğrudan istek) bir kullanıcı kendine ya da başkasına admin rolü
   veremez. ("İlk kayıt olan admin olur" kuralı yarış durumu riski nedeniyle kullanılmaz.)

## User experience
- Flow: Admin rolündeki kullanıcı giriş yapar → üst barda "Ayarlar"ın yanında "Admin" ikonunu görür →
  tıklar → admin iskelet sayfası boş durum metniyle açılır. Kullanıcı rolündeki biri ikonu görmez;
  adresi elle yazarsa "Bu sayfaya erişim yetkiniz yok" görür.
- States:
  - Üst bar: `admin.panel` yetkisi varsa "Admin" ikonu · yoksa ikon yok.
  - Admin iskeleti: boş durum metni (yalnızca admin).
  - Yetkisiz: "Bu sayfaya erişim yetkiniz yok" (sayfa) · "Bu işlem için yetkiniz yok" (işlem).
- Design reference: none — the flow above is the design. `source/*.png` (commit `61cc824`) üst barında
  admin ikonu yok; ikon tasarımdaki öğeleri bozmadan "Ayarlar"ın yanına eklenir. Renk ve tema 0001'deki
  tema değişkenlerinden gelir.
- Accessibility: "Admin" ikonunun erişilebilir adı "Admin"dir, klavyeyle odaklanıp açılabilir; yetkisiz
  durum metni ekran okuyucuya duyurulur; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Yetki kararı her zaman sunucudadır; arayüzde gizleme yalnızca kolaylıktır (`docs/security.md`).
- Admin, kullanıcı verisini görmez (BR-1); bu spec'te admin alanı boştur.
- Arayüz metinleri yalnızca Türkçe.
- Out of scope: admin istatistikleri ve aktiviteleri (0010), kullanıcı listesi ve ban (0024), hata
  bildirimi yönetimi (0025), model yetkilerinin uygulanması (0013), `publish.use`, rol atama/değiştirme
  arayüzü, ikiden fazla rol.
- Split: done — 0011 Roller, yetkiler ve admin iskeleti / kardeşler: 0002, 0012

## Acceptance criteria
- [ ] AC-1 — Kayıt formuyla açılan yeni hesap kullanıcı rolündedir: üst barda "Admin" ikonu görünmez.
- [ ] AC-2 — Kullanıcı rolündeki biri admin alanını adresini yazarak açmaya çalıştığında "Bu sayfaya erişim yetkiniz yok" görür; admin verisini ya da admin işlemini sunucuya doğrudan isteyerek almaya çalıştığında yetkisiz yanıt alır ve hiçbir admin verisi dönmez; arayüzden yapılan yetkisiz işlem "Bu işlem için yetkiniz yok" gösterir.
- [ ] AC-3 — Admin rolündeki kullanıcı üst barda "Ayarlar"ın yanında "Admin" ikonunu görür, ikon klavyeyle açılabilir ve admin iskelet sayfası boş durum metniyle açılır.
- [ ] AC-4 — Ortam ayarındaki e-postayla (büyük/küçük harf farkıyla yazılmış olsa bile; testler `tr-TR` kültüründe) kayıt olan hesap admin rolü alır; API açılışında bu e-postaya ait hesap zaten varsa admin rolü alır; ayar boşken hiçbir hesap admin olmaz; ayardaki e-postadan farklı bir e-postayla kayıt olan hesap kullanıcı rolünde kalır.
- [ ] AC-5 — Kayıt formuna rol alanı eklenerek ya da sunucuya doğrudan gönderilen isteklerle hiçbir kullanıcı kendine ya da başkasına admin rolü veremez; deneme sonrası hesap kullanıcı rolünde kalır (kasıtlı bozma ile kanıtlanır).

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
