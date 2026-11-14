# ADR 0003 — Branch yok: doğrudan `main`'e, yalnızca `Konsept/ANEW/` yollarıyla commit

- Status: Accepted
- Date: 2026-11-14

## Context
ANEW varsayılan olarak her işi ayrı bir branch'te yapıp merge ediyor (`feature/…`, bootstrap
için `chore/bootstrap`) ve default branch'e doğrudan commit'i yasaklıyor (`docs/git.md`
şablonu). Bu proje ise kendi reposu değil: repo kökü `anewcoursedemos` ve **aynı çalışma
klasörünü başka oturumlar da aynı anda kullanıyor** (ör. `TAHA` tarafı). Bir oturum
`git checkout feature/…` yaparsa bütün çalışma klasörü o branch'e geçer; paralel oturumun o an
aldığı commit yanlış branch'e düşer, işler karışır. Ayrıca iki oturum aynı anda commit almaya
çalışabilir (`.git/index.lock`). Remote tanımlı değil, push yok; dolayısıyla PR de yok. Mevcut
git geçmişi de zaten `main` üzerinde. Kaynak: `Promtlar/0012_*.md` (öneri ve gerekçe),
`Promtlar/0013_*.md` (kullanıcı onayı: "branch iptal… ana klasörden main olarak commit
alınacak ama sadece bizim çalışma klasörümüz için"), `Promtlar/0014_*.md` (önemsiz değişiklik
politikası).

## Decision
Hiç branch açılmaz ve branch değiştirilmez; commit'ler repo kökünden (`anewcoursedemos`)
doğrudan `main`'e, yalnızca `Konsept/ANEW/` altındaki yollar isim verilerek
(`git commit -- Konsept/ANEW/<yollar>`) alınır. Bu, ANEW'in "default branch'e doğrudan commit
yasak" kuralından ve bootstrap çıktısının `chore/bootstrap`'e commit'lenmesi kuralından
bilinçli bir sapmadır; bootstrap çıktısı da `main`'e commit'lenir.

Sapmayı güvenli kılan, birlikte bağlayıcı kurallar (`docs/git.md`'de tam hali):
- `git add -A`, `git add .` ve `git commit -a` asla kullanılmaz.
- Commit'ten önce `git status` kontrol edilir; `.git/index.lock` varsa beklenip yeniden
  denenir, lock dosyası **asla silinmez**.
- Push ve remote yok; PR'ın yerini bağımsız, salt-okunur reviewer ve insanın SHIP onayı tutar.
  Önemsiz değişiklikler: bağımsız reviewer diff'e bakar + `scripts/check` yeşil, spec yok, tek
  dar commit (Seçenek 1, uyarlanmış); Pi'ye gönderilecekse patch versiyonu artırılır.
- Commit mesajlarını AI yazar; onaylanmış plan kendi adım commit'lerini kapsar (her commit için
  ayrıca sorulmaz), ship commit'i SHIP onayıyla alınır.
- `Promtlar/` dosyaları ilgili iş commit'ine dahil edilir; iş commit'i yoksa ayrı bir
  `docs(promtlar): …` commit'i alınır.
- Commit mesajı Türkçe, İngilizce tür önekli ve plan referanslı
  (`feat(concepts): … [plan 0003/2]`, düzeltmelerde `[plan 0003/fix-1]`).
- Pi'ye her gönderimde `anew-konsept/vX.Y.Z` etiketi.
- Force push ve geçmişi yeniden yazma yasak; geri alma yalnızca `git revert` ile.

## Consequences
Kazandırdıkları:
- Paralel oturumlar ortak çalışma klasöründe birbirinin branch'ini değiştirmiyor; commit'ler
  yanlış branch'e düşmüyor.
- İsimli yol commit'i sayesinde başka oturumun stage ettiği dosyalar (ör. `TAHA/`) bu projenin
  commit'ine karışmıyor.
- Lock kuralı eşzamanlı commit'te repo bozulmasını ve başka oturumun commit'inin yarıda
  kesilmesini önlüyor.
- Basit, doğrusal bir `main` geçmişi; mevcut geçmişle uyumlu.

Bedelleri:
- Branch izolasyonu yok: yarım kalmış ya da review'dan geçmemiş bir iş de `main` geçmişine
  girer. Güvence, merge öncesi değil commit öncesi kontrollere (bağımsız review, `scripts/check`,
  onaylı plan) kayıyor.
- Bir planın adımları `main`'de art arda commit'lenir; plan iptal edilirse geri alma birden
  fazla `git revert` gerektirebilir.
- Paralel deneme (iki alternatifi branch'lerde yan yana denemek) yapılamıyor.
- Diğer oturumların commit'leri aynı `main` geçmişinde iç içe görünür; geçmişi okurken
  `Konsept/ANEW/` yoluna göre filtrelemek gerekir.
- ANEW şablonunun branch'e dayalı varsayımlarıyla (ör. bootstrap kuralı) her güncellemede
  sapmanın yeniden hatırlanması gerekir; bu ADR o yüzden var.

## Alternatives considered
- **ANEW varsayılanı: iş başına branch + merge, bootstrap için `chore/bootstrap`:** ortak
  çalışma klasöründe `checkout` tüm repoyu etkilediği için paralel oturumların commit'leri yanlış
  branch'e düşer. Reddedildi.
- **Önemsiz değişiklikte review'suz doğrudan commit (Seçenek 2):** hızlı ama "üreten kendi işini
  doğrulamaz" kuralını deliyor. Reddedildi.
- **Her değişiklik için mini-spec (Seçenek 3):** yazım hatası için bile spec; hedeflenen hıza göre
  gereksiz bürokrasi. Reddedildi.

## Revisit triggers
- Proje kendi ayrı reposuna ya da ayrı bir çalışma klasörüne taşınırsa (paralel oturum sorunu
  ortadan kalkar; ANEW varsayılanına dönmek değerlendirilir).
- Bir remote tanımlanır ve push/PR akışı başlarsa.
- Ekip büyür ya da çalışma modu `strict`'e geçerse.
- Review'dan geçmemiş ya da hatalı bir işin `main`'e girmesi tekrar eden bir sorun haline
  gelirse.
