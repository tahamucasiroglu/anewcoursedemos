# Git

> Bootstrap 2026-11-19. Bu proje, üst repo `anewcoursedemos`'un bir alt klasörüdür (`Okul_Yonetim_Uygulama_Odevi/`).
> Remote ve CI yok; push'u kullanıcı aralıklı olarak kendisi yapar. Bilinçli istisna: `main` üzerinde çalışılır (ADR-0002).

## Branching
- **Dal açılmaz; iş `main` üzerinde yürür** (ADR-0002). Gerekçe: üst repo başka projelerle paylaşılıyor; dal
  değiştirmek tüm çalışma ağacını değiştirir ve başka oturumların commit'leri yanlış dala düşebilir.
- ANEW'in "spec yoksa dal yok" kuralının karşılığı: **spec yoksa commit yok** (Promtlar ve bootstrap çıktısı hariç).
- Her spec/plan, işin **başlangıç commit'ini** (`Base commit: <sha>`) kaydeder; review ve verify diff'i
  `git diff <base>..HEAD -- Okul_Yonetim_Uygulama_Odevi/` ile buradan alır.
- Vazgeçilen iş `git revert` ile geri alınır.

## Commits
- Conventional Commits; **açıklama Türkçe**, tür kelimesi ve plan referansı İngilizce (protokol); kapsamın başında `okul/`:
  `feat(okul/yoklama): toplu yoklama ucu eklendi [plan 0003/2]`.
  Review düzeltmeleri tur numarasıyla: `fix(okul/tahsilat): son taksit kuruş farkı düzeltildi [plan 0005/fix-2]`
  — reviewer turları bunlardan sayar (3. turdan sonra R-06).
- Agent commits follow the same standard: the agent writes the message, the human approves it
  through the gate before the commit, not one by one — an approved plan covers its
  `[plan NNNN/n]` and `[plan NNNN/fix-k]` commits, the ship approval the ship commit. Every
  message stays visible in the history. Lite and strict alike.
- Only `./scripts/check`'s own exit code gates a commit or a push. Never pipe its output into
  another command in the chain that decides it: in `./scripts/check | grep … && git commit` the
  chain takes grep's exit code, so a red check commits. Keep the output aside and gate on check
  alone: `./scripts/check > check.log 2>&1 && git commit …` (then show `check.log`).
- Commit named paths only: `git commit -- <paths>`, never `git add -A`, `git add .` or
  `commit -a`. **Yalnızca bu klasörün altındaki yollar**; üst repodaki başka projelerin dosyalarına dokunulmaz.
- **Promtlar kayıtları:** her istek+cevap dosyası ayrı commit: `docs(okul/promtlar): NNNN <konu>`; yalnızca
  `Promtlar/` dosyalarına dokunduğu için ek onay istemez.
- Ajan commit'lerinin sonunda `Co-Authored-By:` satırı bulunur.

## Forbidden
- Push (ajan push yapmaz; kullanıcı yapar).
- Force push, history rewriting. Undo = `git revert` (see recovery R-11).
- `okul-yonetim-platformu-kapsam.md` değiştirilmez; kapsam değişikliği ADR veya spec ile.
- Başka projelerin klasörlerine dokunan commit.
- (ANEW varsayılanı "varsayılan dala doğrudan commit yasak" bu projede ADR-0002 ile kaldırıldı.)

## Trivial changes
- Policy: **(1) uyarlanmış — spec yok, PR yerine bağımsız `reviewer` alt-ajanı** (salt okunur) değişikliğe bakar;
  `scripts/check` yeşil; `main`'e tek, dar commit: `fix(okul/trivial): <açıklama>`.
  (PR yok, çünkü remote yok.)
- Always: one narrow commit, `scripts/check` green. A second file or a new test means re-triage.
- Hassas alanlara (S1–S6) dokunan hiçbir değişiklik trivial sayılmaz.

## Pull requests
- Remote olmadığı için PR yok. Remote eklenirse bu bölüm ve ADR-0002 yeniden değerlendirilir (PR + squash-merge).
