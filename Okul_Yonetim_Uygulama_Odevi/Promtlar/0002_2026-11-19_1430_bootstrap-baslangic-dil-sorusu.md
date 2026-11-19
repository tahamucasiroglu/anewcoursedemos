# 0002 — /bootstrap başlangıcı: dil sorusu (Adım 0 · LANGUAGE)

**Tarih:** 2026-11-19 14:30

## İstek

> /bootstrap (komutla birlikte hazır cevap verilmedi: chat=, docs=, mode=, trivial= yok)

## Cevap

Bootstrap başladı. AGENTS.md, `workflows/bootstrap.md` ve `prompts/bootstrap.md` okundu.
Akış: **DİL → İNCELEME → GÖRÜŞME → ÜRETİM → DOĞRULAMA → RAPOR**. Komutla hazır cevap gelmediği için hepsi tek tek sorulacak.

**Adım 0 — Dil (GATE: insan)** İki ayrı ayar:

- **(a) Görüşme dili:** Önerim **Türkçe** — sohbete Türkçe başladın.
- **(b) Doküman dili** (docs/, spec'ler, planlar, ADR'ler, inceleme raporları): Önerim **Türkçe**.
  Gerekçe: Proje dokümanı Türkçe, hedef pazar Türkiye (KVKK, e-Okul, MEB). Ekip uluslararası değil, İngilizce isteyen bir tedarikçi ya da uyum kuralı da yok.
  Not: ANEW'in protokol alanları (`Status:`, `Approved by / on:`, şablon başlıkları) her durumda İngilizce kalır; araçlar bunlarla çalışır.

**Dokümanda istenen:** Doküman dil hakkında açık bir şey söylemiyor. Ancak doküman tamamen Türkçe ve ürün Türkiye pazarına yönelik: KVKK, e-Okul, MEB, Türkiye'de barındırma seçeneği, Netgsm, iyzico/Paycell. Bu yüzden Türkçe doküman dili dokümanla uyumlu.

Bu karar kalıcı olacak: AGENTS.md'deki `Language:` satırına ve `docs/conventions.md` dosyasına yazılır.

Cevap bekleniyor: `chat=tr docs=tr` uygun mu?
