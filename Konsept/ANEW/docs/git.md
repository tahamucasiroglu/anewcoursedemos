# Git

> Bootstrap sırasında 2026-11-14 tarihinde dolduruldu (kaynak: `Promtlar/0012`–`0014`). ANEW varsayılanları bilinçli olarak gevşetildi; gerekçe aşağıda ve ADR-0003'te.

## Branching
- **Branch yok.** Tüm işler doğrudan `main` üzerinde yapılır; hiçbir oturum `git checkout`/`git switch` ile branch
  değiştirmez. Feature, refactor, fix, incident ve trivial işler için de ayrı branch açılmaz.
- **Bootstrap çıktısı da `main`'e** commit'lenir (ANEW'in `chore/bootstrap` kuralından bilinçli sapma).
- **Sapmanın gerekçesi:** repo kökü `anewcoursedemos` olan çalışma klasörü (working tree) **başka oturumlarla
  ortak ve eşzamanlı** kullanılıyor (ör. `TAHA` tarafı). Bir oturum branch değiştirirse bütün repo o branch'e geçer;
  paralel oturumun o anki commit'i yanlış branch'e düşer. Ortak tree'de branch değiştirmemenin tek güvenli yolu
  doğrudan `main`'dir. Karar kaydı: **ADR-0003** (`docs/decisions/0003-*.md`).
- Commit'ler repo kökünden (`anewcoursedemos`, iki üst klasör) alınır ve **yalnızca `Konsept/ANEW/` yollarını**
  içerir.

## Commits
- Conventional Commits, with a plan reference: `feat(catalog): paging endpoint [plan 0001/3]`.
  Review fixes name their fix round instead of the step:
  `fix(catalog): empty page size [plan 0001/fix-2]` — the reviewer counts rounds from these
  (R-06 after round 3).
- **Bu projede:** tür öneki İngilizce, mesaj **Türkçe**: `feat(concepts): konu önerisi üretimi [plan 0003/2]`,
  review düzeltmesi `[plan 0003/fix-1]`.
- Agent commits follow the same standard: the agent writes the message, the human approves it
  through the gate before the commit, not one by one — an approved plan covers its
  `[plan NNNN/n]` and `[plan NNNN/fix-k]` commits, the ship approval the ship commit. Every
  message stays visible in the branch and the PR. Lite and strict alike.
  *(Bu projede branch ve PR olmadığı için mesajlar `main` geçmişinde görünür kalır.)*
- Only `./scripts/check`'s own exit code gates a commit or a push. Never pipe its output into
  another command in the chain that decides it: in `./scripts/check | grep … && git commit` the
  chain takes grep's exit code, so a red check commits. Keep the output aside and gate on check
  alone: `./scripts/check > check.log 2>&1 && git commit …` (then show `check.log`).
- Commit named paths only: `git commit -- <paths>`, never `git add -A`, `git add .` or
  `commit -a`. Another session or tool may have written or staged files in the same tree.
- **Bu projede yol biçimi:** repo kökünden `git commit -- Konsept/ANEW/<yollar>`; `Konsept/ANEW/` dışındaki hiçbir
  yol commit'e girmez.
- **Eşzamanlı commit:** commit'ten önce `git status` kontrol edilir. `.git/index.lock` varsa (başka bir oturum commit
  alıyor) beklenip tekrar denenir; **lock dosyası asla silinmez.**
- **Prompt kayıtları:** `Promtlar/` dosyaları ilgili iş commit'ine dahil edilir; iş commit'i yoksa ayrı bir
  `docs(promtlar): …` commit'i alınır.
- **Push yok**, remote tanımlı değil.
- **`source/` klasörü** (proje kaynakları: görseller, ikon, videolar) repoya girer. Kök `.gitignore`daki `Source/` satırı
  Windows'ta (core.ignorecase) onu da dışladığı için `Konsept/ANEW/.gitignore` içindeki `!source/` ile geri alınır —
  bu satır silinmez; klasör `git commit -- Konsept/ANEW/source/...` ile isim verilerek commit'lenir.

## Forbidden
- Branch açmak ya da branch değiştirmek (`main` dışında çalışmak) — bkz. Branching.
- `Konsept/ANEW/` dışındaki yolları commit'lemek; `git add -A`, `git add .`, `commit -a`.
- `.git/index.lock` dosyasını silmek.
- Push (remote yok).
- Force push, history rewriting on shared branches. Undo = `git revert` (see recovery R-11).
- *(ANEW'in "Direct commits to the default branch" yasağı bu projede kaldırıldı; `main`'e doğrudan commit tek
  onaylı yoldur — ADR-0003.)*

## Trivial changes
Yalnızca change-request triage rubriğinin (`workflows/change-request.md`) TRIVIAL saydığı istekler için geçerlidir:
hiçbir kabul kriteri değişmez, ortak koda dokunulmaz (ör. "hata bıldır" → "hata bildir").
- Policy: **(1) uyarlanmış — bağımsız reviewer + check yeşil, spec yok.** PR yerine bağımsız, salt-okunur reviewer
  alt ajanı diff'i inceler; `scripts/check` yeşil olur; spec yazılmaz; `main`'e tek dar commit alınır.
  Seçilmeyen alternatifler: (2) review olmadan doğrudan commit (üreten kendi işini doğrular, kural 3'ü deler) ·
  (3) her şey mini-spec'ten geçer (bu proje için gereksiz bürokrasi).
- Always: one narrow commit, `scripts/check` green. A second file or a new test means re-triage.
- Pi'ye gönderilecekse **patch versiyonu artırılır** (ör. `0.1.20 → 0.1.21`).

## Pull requests
- **PR yok** (remote yok, branch yok). PR'ın yerini ANEW'in **bağımsız review'ı** (ayrı, salt-okunur reviewer) ve
  insanın **SHIP onayı** tutar. PR template checklist'i ve squash-merge bu projede uygulanmaz; `scripts/check`
  yeşil şartı commit öncesinde aynen geçerlidir.

## Versioning & tags
- Tek ürün versiyonu (Web, API, Ajan ortak) `Directory.Build.props` içinde, `0.1.0`'dan başlar. Küçük düzeltme →
  patch; sayfa ekleme ya da kapsamlı değişiklik → minor (idareli); `1.0.0` = tam testli, çalışan ürün.
  Ayrıntı: `docs/conventions.md`.
- Pi'ye **her gönderimden önce** versiyon artırılır ve gönderilen commit etiketlenir: `anew-konsept/vX.Y.Z`
  (ör. `anew-konsept/v0.1.3`). Önek, repodaki diğer projelerin etiketleriyle karışmayı önler. Etiketler push edilmez.
