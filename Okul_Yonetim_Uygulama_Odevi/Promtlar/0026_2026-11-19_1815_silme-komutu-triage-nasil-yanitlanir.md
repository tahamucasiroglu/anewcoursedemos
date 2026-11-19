# 0026 — Şablon artıklarını silme komutu · Düzeltmeler için ne denir (ANEW)

**Tarih:** 2026-11-19 18:15

## İstek

> silme için cmd kodu ver çalıştırayım. birde bu düzeltmeler için ne demem lazım mesela new-feature ile mi yazmam lazım ne demem lazım anew standartlarında.

## Cevap

### Silme komutu (Windows cmd)
```bat
cd /d C:\Dosyalar\Projects\engin_demirog\anewcoursedemos\Okul_Yonetim_Uygulama_Odevi
del /q mobile\AGENTS.md mobile\LICENSE web\README.md web\.oxlintrc.json web\src\App.css web\public\icons.svg
rmdir /s /q mobile\.claude web\src\assets
```
Claude Code içinden çalıştırmak istersen (`!` bash'te çalışır):
```
! rm -rf mobile/AGENTS.md mobile/LICENSE web/README.md web/.oxlintrc.json web/src/App.css web/public/icons.svg mobile/.claude web/src/assets
```
Bu dosyalar hiç commit edilmedi; silince git'te iz kalmaz.

### Düzeltmeler için ne demelisin? — Yeni komut gerekmez
- Şu an hâlâ **aynı `/new-feature` akışının içindeyiz**, 3. duraktayız: **TRIAGE** (bulguları ayıklama).
- ANEW'de inceleme bulguları yeni bir özellik değildir. Aynı işin **düzeltme turudur** (fix round). Spec ve plan aynı kalır.
- Yapman gereken tek şey bu durağa cevap vermek:
  - **Hepsini kabul:** `onaylıyorum` → önerdiğim kararların hepsi geçerli olur.
  - **Bazılarını değiştirmek:** Ör. `onaylıyorum ama 6 ertele, 13 gürültü`. Bulgu numarası + karar yazman yeterli.
    Kararlar: **düzelt** (gerçek) · **ertele** (backlog'a) · **gürültü** (gerek yok, gerekçesi yazılır) · **incele** (önce araştır).
- Sonrasında ben sırayla şunları yaparım:
  1. Kararları plandaki **Triage** tablosuna yazarım.
  2. Her bulguyu ayrı commit'le düzeltirim: `fix(okul/...): ... [plan 0001/fix-1]`.
  3. Yeniden bağımsız inceleme yapılır.
  4. Doğrulama yapılır.
  5. 4. durak **SHIP**: senin teslim onayın.

### Hangi komut ne zaman? (ANEW özeti)
| Durum | Komut |
|---|---|
| Yeni bir özellik | `/new-feature "..."` |
| Yarıda kalan işe dönmek (oturum kapandıysa) | Aynı `/new-feature "..."` komutu. Açık spec'i bulur, kaldığı duraktan devam eder; yeni spec açmaz. |
| **Teslim edilmiş** bir davranışı değiştirmek | `/change "<istek> <iş>"` |
| Teslim edilmiş bir şeyde **hata** | `/fix-bug` |
| Davranışı değiştirmeden kod düzenleme | `/refactor` |
| Bir şeyler ters gitti | `/recover` |

Özetle: Şu an komut yazmana gerek yok, `onaylıyorum` (veya numara + karar) demen yeterli.
