# 0017 — `.git` kaldırıldı · Konu 11 onayı · Konu 12/12: Çalışma modu (lite / strict)

**Tarih:** 2026-11-19 15:27

## İstek

> (önce `! Remove-Item -Recurse -Force .\.git` denendi — `!` bash'te çalıştığı için olmadı; bash karşılığı `rm -rf .git` önerildi)
>
> elle sildim. 1 numaralı cevabı onaylıyorum

## Cevap

**`.git` kaldırıldı — doğrulandı:** Klasör artık üst repoya bağlı (`git rev-parse --show-toplevel` → `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos`). ANEW dosyaları üst repoda "izlenmeyen (untracked)" olarak görünüyor; ÜRETİM sonrası commit edilecek.

**Konu 11 — Trivial politikası: ONAYLANDI → (1) uyarlanmış.** Spec yok; bağımsız `reviewer` alt-ajanı bakar; `scripts/check` yeşil; `main`'e tek dar commit. Hassas alanlara dokunan değişiklik trivial sayılmaz.

**Konu 12/12 — Çalışma modu** (AGENTS.md `Mode:`)

| | **lite** | **strict** |
|---|---|---|
| Nasıl çalışır | `/new-feature "<iş>"` tek komutla ANALYZE → PLAN → BUILD → REVIEW → VERIFY → SHIP segmentlerini zincirler, **her kapıda durup onayını ister** | Her segment ayrı komut ve **ayrı oturum/rol** (`/analyze`, `/plan`, `/build`...); her biri durur ve el değiştirir |
| Uygun olduğu | Tek kişi / küçük ekip, hızlı ilerleme | Büyük ekip, rol ayrımı zorunlu, düzenlemeye tabi ortam |
| Değişmeyen güvenceler | Spec olmadan kod yok, plan onayı, **bağımsız review (salt okunur alt-ajan)**, kanıt tablosu, korunan testler | Aynı |

**Önerim: lite.**
Gerekçe:
- Tek geliştirici + AI var; strict'in "her rol ayrı oturum" maliyeti burada fayda getirmez.
- Hassas alanlar (S1–S6) lite modda da **otomatik olarak sıkılaşır**: clarify kapısı her zaman sorulur, her düzeltme turu yeniden incelenir, her kriter kasıtlı bir bozmayla "kırmızıya düşüyor mu" diye ispatlanır. Yani güvenlik isteyen yerlerde strict'e yakın davranış zaten var.
- İleride ekip büyürse mod `AGENTS.md`'de tek satırla strict'e çevrilebilir.

**Dokümanda istenen:** Doküman süreç modu hakkında bir şey söylemiyor. Dolaylı olarak, KVKK/güvenliğin "MVP'den itibaren tam" olması isteği, hassas alan sıkılaştırmasıyla karşılanıyor.

Bu son görüşme sorusu. Onaydan sonra **ÜRETİM** adımı başlar: `docs/*.md`, `AGENTS.md`, `scripts/check.conf` (greenfield olduğu için yorum satırı halinde planlanan adımlar) üretilir, sonra doctor/check/residue çalıştırılır ve rapor gelir.

Cevap bekleniyor: Mod = lite uygun mu?
