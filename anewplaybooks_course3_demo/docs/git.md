# Git

> Bootstrap'ta ayarlandı (2026-11-21). Bu klasör, üst klasördeki ortak reponun
> (`anewcoursedemos`) bir alt klasörü. Aynı çalışma ağacında başka oturumlar başka klasörlere
> commit atıyor. Remote'a push yok, PR ve CI yok.

## Branching
- **Branch açılmaz. Commit'ler `main`'e gider**, bootstrap çıktısı dahil (`chore/bootstrap` yok).
  Bu, ANEW'in "direct commits to the default branch" yasağının **bilinçli olarak gevşetilmesi**.
  Gerekçe:
  - (1) Ortak çalışma ağacında branch değiştirmek diğer oturumların dosyalarını da etkiler.
  - (2) PR ve CI olmadığı için branch'in sağladığı güvenceler (inceleme, CI) zaten yok.
  - (3) Kapılar dosyalarla korunuyor: spec onayı, plan onayı, salt-okunur review, verify tablosu.
- İş birimi branch adıyla değil, commit mesajındaki plan referansıyla izlenir.

## Commits
- Commit'ler **üst repo kökünden** (`C:\Dosyalar\Projects\engin_demirog\anewcoursedemos`)
  atılır ve yalnız bu klasörün yollarını içerir:
  `git add -- anewplaybooks_course3_demo/<yol>` ve `git commit -- anewplaybooks_course3_demo/<yol>`.
  `git add -A`, `git add .` ve `commit -a` **asla** kullanılmaz: başka oturumların dosyaları
  stage'e karışır.
- Conventional Commits, Türkçe açıklama, scope `course3`, plan referansıyla:
  `feat(course3): bekleyen talep iptali [plan 0001/3]`. Review düzeltmeleri fix turunu yazar:
  `fix(course3): eşik mesajı [plan 0001/fix-1]` (reviewer turları bunlardan sayar, R-06 tur 3'ten
  sonra).
- Agent commit'leri de aynı standarda uyar: mesajı agent yazar, insan commit'ten önce kapıda
  onaylar, commit commit değil. Onaylanan plan kendi `[plan NNNN/n]` ve `[plan NNNN/fix-k]`
  commit'lerini, ship onayı da ship commit'ini kapsar.
- Commit kararını yalnız `./scripts/check`'in kendi çıkış kodu verir. Çıktısı, kararı veren
  zincirde başka bir komuta pipe edilmez. Çıktı kenara alınır, karar yalnız check'e bağlanır:
  `./scripts/check > check.log 2>&1 && git commit …` (sonra `check.log` gösterilir. Log repo
  dışında durur ya da silinir, asla commit edilmez).
- Script dosyaları executable bit'iyle eklenir: `git add --chmod=+x anewplaybooks_course3_demo/scripts/<ad>`.

## Forbidden
- **Push** (bu repoda push sahibinin kararıdır, agent push atmaz).
- Force push, geçmişi yeniden yazma (amend dahil, paylaşılan `main` üzerinde). Geri alma
  `git revert` ile (recovery R-11).
- Branch değiştirmek veya ortak çalışma ağacının durumunu değiştiren komutlar (checkout, stash,
  reset) insan onayı olmadan kullanılmaz.

## Trivial changes
<!-- Applies only to requests the change-request triage rubric
     (workflows/change-request.md) classifies as TRIVIAL: no acceptance criterion changes, no shared code. -->
- Policy: **(1) PR + one reviewer, no spec**, bu ortama uyarlanmış hali. PR olmadığı için
  "bir reviewer" **salt-okunur reviewer subagent'ıdır**: diff'i inceler, ardından
  `./scripts/check` yeşilse `main`'e tek dar commit atılır.
- Her zaman: tek dar commit ve yeşil `scripts/check`. İkinci bir dosya veya yeni bir test gerekirse yeniden triyaj yapılır.

## Pull requests
- Yok (remote'a push ve PR yok). PR'ın yerini dosyadaki kapılar ve SHIP kapısındaki gösterim
  alır (`docs/testing.md`, Evidence policy 3).
