# Spec 0026 — Güncel online modeller

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0026-plan.md`
- Self-critique: done (Approved under the user's explicit request (2026-11-17))
- Source: kullanıcı isteği 2026-11-17 (`Promtlar/0059`) — "claude ve diğer online llm modellerinde son sürümleri kullanalım. buna göre sürümü güncelle ve updateleri at"
- Approval: Approved under the user's explicit request (2026-11-17)
- Sensitive area: yok (API key / yetki davranışı değişmez; yalnızca katalog verisi ve eski kimliğin yerine geçeni)
- Depends on: 0004, 0005, 0006, 0013

## Intent
Katalog (0004) eski model sürümleriyle yazılmıştı (Claude 4.5, GPT-5, gpt-image-1, GLM-4.6, Kimi K2); bunların bir kısmı
sağlayıcıda kapandı ya da kapanmak üzere. Kullanıcı her online sağlayıcıda en güncel modelleri seçebilmeli, fiyatlar resmî
sayfalardan gelmeli. Eski model kimliğini profil ayarlarında kayıtlı tutan kullanıcı hiçbir şey yapmadan güncel karşılığına
geçmeli; ayar kaydı (B-112 benzeri) eski kimlik yüzünden reddedilmemeli. Yerel girişler (0006) değişmez. Yeni sağlayıcı
(ör. Google Gemini) eklenmez — katalogda yok, ayrı iş.

## Requirements
1. Online modeller güncel sürümlerle değişir (kimlik, API adı, efor seviyeleri, çıktı limiti, fiyat):
   - **Anthropic:** Claude Opus 5.5, Sonnet 5.5, Haiku 5.5.
   - **OpenAI metin:** GPT-6 Astra, GPT-6.1 Sol, GPT-6.1 Terra. **OpenAI görsel:** gpt-image-2.6-flare.
   - **OpenRouter:** Claude Sonnet 5.5 ve Claude Opus 5.5 (OpenRouter).
   - **GLM:** GLM-5.3. **Kimi:** Kimi K3.1 Pro.
2. Kaldırılan her eski kimliğin katalogda tek bir **yerine geçen** kimliği vardır (aynı tür). Eski kimlik listelerde görünmez.
3. Kayıtlı ayarlardaki (konsept ayarları: konsept/tekrar/konu rolleri; üretim ayarları: kod ve görsel modeli) eski kimlikler
   sürüm açılışında yerine geçen kimliğe taşınır (kullanıcı bir şey yapmaz); efor değeri yeni modelde geçerli kalır (her eski
   eforun yeni modelde karşılığı var). Böylece eski kimlikli profilde ayar kaydı reddedilmez.
4. Eski kimlikle gelen bir çağrı (ör. daha önce sabitlenmiş, durdurulmuş/başarısız işin devamı ya da sürüm üretimi) yerine
   geçen modele gider; harcama yeni modelin kimliği ve fiyatıyla kaydedilir. Eski efor yeni modelde yoksa model varsayılanı
   kullanılır.
5. Kodda, testlerde ve belgelerde varsayılan ya da örnek olarak kullanılan model kimlikleri güncel kimliklere işaret eder.

### Model listesi ve kaynaklar (2026-11-17)
| Kimlik | API adı | Efor | Çıktı | Fiyat (USD) | Kaynak |
|---|---|---|---|---|---|
| anthropic/claude-opus-5-5 | claude-opus-5-5 | low/medium/high/max, vars. medium | 128000 | 4 / 20 | Anthropic (claude-api skill, model tablosu 2026-11-03) |
| anthropic/claude-sonnet-5-5 | claude-sonnet-5-5 | low/medium/high/max, vars. high | 128000 | 2 / 10 | aynı |
| anthropic/claude-haiku-5-5 | claude-haiku-5-5 | low/medium/high/max, vars. medium | 128000 | 0.10 / 0.50 (≤100K istem; üstü 0.50 / 2.50) | aynı |
| openai/gpt-6-astra | gpt-6-astra | low/medium/high/max, vars. medium | 128000 | 10 / 50 | developers.openai.com/api/docs/pricing, /models/gpt-6-astra |
| openai/gpt-6.1-sol | gpt-6.1-sol | low/medium/high/max, vars. medium | 128000 | 2 / 10 | aynı, /models/gpt-6.1-sol |
| openai/gpt-6.1-terra | gpt-6.1-terra | low/medium/high/max, vars. medium | 128000 | 1 / 5 | aynı, /models/gpt-6.1-terra |
| openai/gpt-image-2.6-flare | gpt-image-2.6-flare | — | — | ~0.044 / görsel (medium 1024²) | resmî: 30 USD / 1M görsel çıktı tokenı (/models/gpt-image-2.6-flare); görsel başı ~1.449 token üçüncü taraf ölçümü (tahmin) |
| openrouter/anthropic/claude-sonnet-5.5 | anthropic/claude-sonnet-5.5 | — | 128000 | 2 / 10 | openrouter.ai/anthropic/claude-sonnet-5.5 |
| openrouter/anthropic/claude-opus-5.5 | anthropic/claude-opus-5.5 | — | 128000 | 4 / 20 | openrouter.ai/anthropic/claude-opus-5.5 |
| glm/glm-5.3 | glm-5.3 | — | 128000 | 1.40 / 4.40 | docs.z.ai/guides/overview/pricing, /guides/llm/glm-5.3 |
| kimi/kimi-k3.1-pro | kimi-k3.1-pro | — | 131072 | 2 / 10 | platform.kimi.ai quickstart (model/uç); fiyat tablosu sayfada çizilmedi → ikincil kaynaklar (qcode.cc, yottalabs.ai) resmî liste fiyatını aktarıyor |

### Yerine geçenler
| Eski | Yeni | Gerekçe |
|---|---|---|
| anthropic/claude-opus-4-5 | anthropic/claude-opus-5-5 | aynı sınıf |
| anthropic/claude-sonnet-4-5 | anthropic/claude-sonnet-5-5 | aynı sınıf |
| anthropic/claude-haiku-4-5 | anthropic/claude-haiku-5-5 | aynı sınıf |
| openai/gpt-5 | openai/gpt-6.1-sol | OpenAI deprecations: gpt-5 kapanış 2026-12-11, önerilen gpt-6.1-sol |
| openai/gpt-5-mini | openai/gpt-6.1-terra | aynı sayfa: önerilen gpt-6.1-terra |
| openai/gpt-image-1 | openai/gpt-image-2.6-flare | gpt-image-1 2026-10-23'te kapandı; güncel hızlı görsel modeli |
| openrouter/anthropic/claude-sonnet-4.5 | openrouter/anthropic/claude-sonnet-5.5 | aynı sınıf |
| glm/glm-4.6 | glm/glm-5.3 | güncel amiral |
| kimi/kimi-k2 | kimi/kimi-k3.1-pro | K2 serisi 2026 sonunda kapanıyor; K2 geçiş rehberi K3.1 Pro'yu gösteriyor |

## User experience
none — no new user interface. Ayarlar/Modeller listesi ve model seçim kutuları yeni girişleri gösterir; eski kimlikli profilde
seçim kutusu yerine geçen modeli seçili gösterir.

## Constraints & out of scope
- 0004 AC-1 sınırları korunur: metin sağlayıcısı başına 1–3 model, tek online görsel model (OpenAI), efor low/medium/high/max.
  Bu yüzden Claude Fable 5.2 (8/40 USD), GPT-6 Luna (0.10/0.50), gpt-image-2 ve `xhigh` eforu eklenmedi; istenirse 0004 AC-1
  güncellenerek ayrı işte eklenir.
- Yeni sağlayıcı (Google Gemini vb.) ve yeni lehçe yok; istek gövdeleri değişmez (Anthropic `output_config.effort`, OpenAI
  `reasoning_effort` yeni modellerde geçerli; GLM/Kimi efor göndermez).
- Görsel çağrısı hâlâ görsel başı sabit fiyatla kaydedilir (token bazlı görsel maliyeti ayrı iş).
- Geçmiş kayıtlar (harcama, üretilmiş video sürümleri, iş geçmişi) eski kimliği korur — gerçekte o model kullanıldı.
- `Identity__ModelPermissions__*` ile eski kimliğe kısıtlanmış yetkiler taşınmaz (Pi'de ayar yok; varsayılan `model.use:*`).
  Kısıtlı kurulumda yöneticinin yeni kimliği yazması gerekir.

## Acceptance criteria
- [ ] AC-1 — Katalog API'si Req 1'deki online modelleri tablodaki API adı, efor, çıktı limiti ve fiyatla döner; eski kimlikler listede yok; yerel girişler değişmez.
- [ ] AC-2 — Her yerine geçen eşlemesi katalogda var olan, aynı türden, kendisi yerine geçilmemiş bir girişe işaret eder; eski kimlik katalog girişi olarak kalmaz.
- [ ] AC-3 — Eski kimlikle yapılan çağrı yerine geçen modelin API adıyla sağlayıcıya gider, harcama yeni kimlikle kaydedilir; yeni modelde olmayan eski efor varsayılana döner.
- [ ] AC-4 — Taşıma: eski kimlikli konsept ayarı (üç rol) ve üretim ayarı (kod, görsel) satırları açılış taşımasından sonra yerine geçen kimliği taşır; efor ve diğer değerler değişmez; eski kimliği olmayan satır değişmez.
- [ ] AC-5 — Taşınmış (yeni kimlikli) profilde konsept ayarı kaydı ve yalnızca Altyazı değiştirilen üretim ayarı kaydı başarılı olur; eski kimliğin yerine geçeni seçilebilir listededir.
- [ ] AC-6 — Kod/test/belge varsayılanları güncel kimliklerdir (eski kimlik yalnızca yerine geçen eşlemesinde, taşımada ve bunları doğrulayan testlerde geçer).

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise / deferred | |
| Regressions introduced | |
| Bugs escaped to production | |
