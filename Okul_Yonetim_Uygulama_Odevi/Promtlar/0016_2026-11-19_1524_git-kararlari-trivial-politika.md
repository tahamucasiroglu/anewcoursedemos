# 0016 — Konu 10: Git kararları güncellendi · Konu 11/12: Küçük değişiklik (trivial) politikası

**Tarih:** 2026-11-19 15:24

## İstek

> remote ve ci olmasın. commit mesajları türkçe olsun orada yanlış karar vermişim. commitleri buradan almayacaksın. C:\Dosyalar\Projects\engin_demirog\anewcoursedemos   üst klasör olan buradan alacaksın repom burada. commitleri alırsın ben pushlarım aralıklı. Main üzerinden devam edebilirisin dal açmaya gerek olmayabilir burada karar senin. kalanları onaylıyorum.

## Cevap

**Konu 10 — Git: DEĞİŞİKLİKLERLE ONAYLANDI.**

1. **Repo = üst klasör `anewcoursedemos`.** Bu klasörde kurulumda açtığım iç içe `.git` (boş, hiç commit yok) kaldırılmalı. Silme komutu izin aşamasında reddedildi; kullanıcıdan onay/çalıştırma istendi (aşağıda).
   ANEW betikleri (`doctor`, `gate`) alt klasörde çalışacak şekilde yazılmış (`git -C`, `--relative`, `-- .`). Üst repoda sorun çıkarmaz.
2. **Remote ve CI yok.** Push'u kullanıcı aralıklı olarak kendisi yapar; ajan push yapmaz. Tam (full) yeşil, yerelde `scripts/check` ile alınır.
3. **Commit mesajları Türkçe.** Conventional Commits'in tür kelimesi ve plan referansı protokol olduğu için İngilizce kalır; açıklama Türkçe olur. Üst repoda başka projeler de olduğu için kapsamın başına `okul/` eklenir:
   - `feat(okul/yoklama): toplu yoklama ucu eklendi [plan 0003/2]`
   - `fix(okul/tahsilat): son taksitte kuruş farkı düzeltildi [plan 0005/fix-1]`
   - `docs(okul/promtlar): 0016 git kararları`
4. **Dal kararı (bana bırakıldı): `main` üzerinden, dal açmadan.** Gerekçe:
   - Üst repo başka projelerle (Konsept vb.) paylaşılıyor. Dal değiştirmek (`checkout`) **tüm çalışma ağacını** değiştirir; o sırada başka bir oturum Konsept'e commit atarsa, commit yanlışlıkla bizim dalımıza düşer. `main`'de kalmak bu riski ortadan kaldırır.
   - Push'u sen `main`'den yapıyorsun; ayrıca birleştirme adımı gerekmez.
   - ANEW güvencesi kaybolmaz: Her feature'ın başlangıç commit'i spec/plan'da kayıtlı olur. Review bu commit'ten itibaren yalnızca bu klasörün diff'ine bakar (`git diff <başlangıç>..HEAD -- Okul_Yonetim_Uygulama_Odevi/`). Commit'ler her zaman yalnızca bu klasördeki adı verilen dosyalarla atılır (`git commit -- <yollar>`).
   - `docs/git.md`'deki "varsayılan dala doğrudan commit yasak" maddesi bu proje için bilinçli bir istisna olarak yazılır, gerekçesiyle.
   - Bedeli: Yarım kalmış bir iş `main`'de görünür. Önlem: Bir plan adımı yalnızca `scripts/check` yeşilken commit edilir; vazgeçilen iş `git revert` ile geri alınır.
5. Kalan maddeler onaylandı: Spec yoksa iş yok; force push/geçmiş yeniden yazma yok; commit'i yalnızca check'in çıkış kodu belirler; Promtlar commit'leri onaysız atılır; kapsam dokümanı değiştirilmez.

**Konu 11/12 — Küçük değişiklik (trivial) politikası** (`docs/git.md` "Trivial changes")

"Trivial" = Etiket/yazım hatası seviyesinde değişiklik: hiçbir kabul kriterini değiştirmeyen, paylaşılan koda dokunmayan, tek dosyalık iş (ör. bir Türkçe mesajdaki yazım hatası). ANEW'in 3 seçeneği:

| Seçenek | Ne demek | Bu proje için durum |
|---|---|---|
| (1) PR + bir reviewer, spec yok | Varsayılan öneri | **PR yok** (remote yok), birebir uygulanamaz |
| (2) Doğrudan commit (trivial dalında) | En hızlı | Dal yok; `main`'e doğrudan commit olur |
| (3) Her şey mini-spec'ten geçer | En sıkı | Bir yazım hatası için ağır |

**Önerim: (1)'in bu projeye uyarlanmış hali.** Spec gerekmez. Ama commit öncesi **bağımsız `reviewer` alt-ajanı** (salt okunur) değişikliğe tek bir göz atar ve `scripts/check` yeşil olur, sonra `main`'e tek, dar bir commit atılır: `fix(okul/trivial): ...`. İkinci bir dosya veya yeni bir test gerekirse iş trivial sayılmaz, normal akışa döner.
Gerekçe: PR'ın sağladığı "ikinci göz" güvencesi korunur ve maliyeti düşük kalır. Hassas alanlara (S1–S6) dokunan hiçbir değişiklik trivial sayılmaz.

**Dokümanda istenen:** Doküman bu konuda bir şey söylemiyor; süreç kararı.

### Kullanıcıdan istenen işlem
İç içe `.git` klasörünü kaldırmak için (silme izni reddedildiği için): `! Remove-Item -Recurse -Force .\.git` (bu klasörde) — ya da bana izin ver, ben sileyim.

Cevap bekleniyor: Trivial politikası (uyarlanmış 1) uygun mu? `.git` silme işlemi.
