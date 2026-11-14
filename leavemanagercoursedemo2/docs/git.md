# Git

> Bootstrap 2026-11-14. ANEW varsayılanları bu ortam için bilinçli olarak gevşetildi (Branching, Forbidden, Trivial changes).

## Branching
- **Branch yok — tüm işler doğrudan `main`'e commit edilir.** Bilinçli gevşetme (bootstrap 2026-11-14, kullanıcı onayı):
  bu proje `anewcoursedemos` reposunun alt klasörüdür; aynı çalışma ağacında başka oturumlar da commit atar, bu yüzden
  `git checkout <branch>` diğer alanları da taşır. Remote, push ve PR yok. Kapı kayıtları dosyalarda tutulur (spec `Status`,
  plan `Approved by / on`); PR'ın yerini bunlar ve bağımsız review tutar.
- Commit'ler repo kökünden (`anewcoursedemos/`) atılır.

## Commits
- Conventional Commits, with a plan reference, **`LM2` önekiyle** (paylaşılan log'da bu alanı ayırt eder):
  `LM2 feat(leave): talep oluşturma [plan 0001/3]`. Açıklama Türkçe; tip ve kapsam İngilizce.
  Review fixes name their fix round instead of the step:
  `LM2 fix(leave): boş tarih aralığı [plan 0001/fix-2]` — the reviewer counts rounds from these
  (R-06 after round 3).
- Agent commits follow the same standard: the agent writes the message, the human approves it
  through the gate before the commit, not one by one — an approved plan covers its
  `[plan NNNN/n]` and `[plan NNNN/fix-k]` commits, the ship approval the ship commit. Every
  message stays visible in `main`'s log (`git log --grep '^LM2'`). Lite and strict alike.
- Only `./scripts/check`'s own exit code gates a commit (trivial changes excepted, see below). Never pipe its output into
  another command in the chain that decides it: in `./scripts/check | grep … && git commit` the
  chain takes grep's exit code, so a red check commits. Keep the output aside and gate on check
  alone: `./scripts/check > check.log 2>&1 && git commit …` (then show `check.log`).
- Commit named paths only: `git commit -- <paths>`, never `git add -A`, `git add .` or
  `commit -a`. Another session or tool may have written or staged files in the same tree.
  Burada yollar her zaman `leavemanagercoursedemo2/` altındadır. Commit'ten önce `git status` ile başka alanlardan
  stage'lenmiş dosya olup olmadığına bakılır; varsa dokunulmaz (unstage edilmez), commit yalnız kendi yollarımızla atılır.

## Forbidden
- Push (remote yok; bootstrap kararı).
- Force push, history rewriting, `reset --hard`. Undo = `git revert` (see recovery R-11).
- Başka alanların dosyalarını stage'lemek, unstage etmek ya da commit'lemek; paylaşılan ağaçta branch değiştirmek.

## Trivial changes
<!-- Applies only to requests the change-request triage rubric (workflows/change-request.md)
     classifies as TRIVIAL: no acceptance criterion changes, no shared code. -->
- Policy: **doğrudan commit, prosedür yok** (bootstrap 2026-11-14, kullanıcı kararı; ANEW seçenek (2)'nin branch'siz hali):
  spec, review ve `scripts/check` şartı yok. Tek ve dar bir commit: `LM2 <tip>(<kapsam>): <açıklama> [trivial]`.
- Bilinen risk: kod içindeki metin düzeltmesi bile derlemeyi bozabilir; bir sonraki `scripts/check` yakalar.
- İkinci bir dosya veya yeni bir test gerekiyorsa iş trivial değildir → yeniden triage.

## Pull requests
- Yok (remote yok). Kanıt yerel `./scripts/check` full çalıştırmasıdır (`docs/testing.md`, Two speeds rule 6).
