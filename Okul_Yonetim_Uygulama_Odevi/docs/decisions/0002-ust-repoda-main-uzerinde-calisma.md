# ADR 0002 — Üst repoda, `main` üzerinde, dal açmadan çalışma

- Status: Accepted
- Date: 2026-11-19

## Context
Proje, kullanıcının `anewcoursedemos` reposunun bir alt klasörü. Aynı repoda başka projeler (Konsept vb.) ve
onlarda çalışan başka oturumlar var. Remote ve CI bu proje için kullanılmıyor; push'u kullanıcı aralıklı yapıyor.
ANEW varsayılanı `feature/<spec>` dalları ve "varsayılan dala doğrudan commit yasak" diyor (`docs/git.md`).
Kullanıcı dal kararını ajana bıraktı.

## Decision
İş `main` üzerinde, dal açmadan yürür. Her spec/plan başlangıç commit'ini (`Base commit:`) kaydeder; review
ve verify diff'i bu commit'ten itibaren yalnızca bu klasörden alır. Commit'ler yalnızca adı verilen ve bu
klasörün altındaki yollarla, yalnızca `scripts/check` yeşilken atılır.

## Consequences
- Kazanç: `checkout` tüm çalışma ağacını değiştirmez; başka oturumların commit'leri yanlış dala düşmez; birleştirme adımı yok.
- Bedel: yarım iş `main`'de görünür; dal düzeyinde izolasyon yok. Önlem: plan adımı yalnızca check yeşilken commit edilir; vazgeçme `git revert`.
- Bedel: başka projelerin commit'leri araya girebilir; diff her zaman klasör yoluyla süzülür.

## Alternatives considered
- Kısa ömürlü yerel `feature/*` dalları + `--no-ff` merge — paylaşılan çalışma ağacında başka oturumlarla çakışma riski; reddedildi.
- Bu klasörde ayrı bir git repo — kullanıcı commit'lerin üst repoda olmasını istedi; reddedildi.

## Revisit triggers
- Proje ayrı bir repoya taşınırsa, remote/CI eklenirse veya ikinci bir geliştirici katılırsa (PR + dal akışına dönüş).
