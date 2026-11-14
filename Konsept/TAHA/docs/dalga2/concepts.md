# Dalga 2 — concepts (Konseptler modülü + Konseptler / Silinenler sayfaları)

The Concepts module and the Konseptler and Silinenler pages are built and committed. The module projects, Api host and Web host build with 0 warnings and 0 errors. The 52 Concepts unit tests, 17 architecture tests and 3 integration tests pass. The API was started on port 7934 against the dev database: it applied the Concepts migration and became ready, and the concept routes probed answered 401 without a login.

Not yet tested logged-in: no real LLM call has been made, and the two pages have not been opened in a browser. The dev database still has no admin account.

**Commits:** `d4f334b` (contracts, Domain, Application), `2b7376c` (Infrastructure, migration, Api, tests, module registration), `e44f972` (Web).

**Public interfaces for other modules** (namespace `TahaKonsept.Modules.Concepts.Application.Abstract`, scoped):
```csharp
public interface IConceptReader { Task<ConceptDetailDto?> GetAsync(Guid userId, Guid conceptId, CancellationToken ct = default); }
public interface IConceptPronunciationSource { Task<IReadOnlyList<PronunciationEntry>> GetPronunciationsAsync(Guid userId, Guid conceptId, CancellationToken ct = default); }
```
- Services behind the controllers: `IConceptService`, `IConceptSettingsService`, `ITopicSuggestionService`, `IConceptGenerationService`, `IConceptRevisionService`.
- `ConceptsStatsContributor` feeds the admin panel `concepts.total`, plus counts for generated, approved and deleted.

**What it does**
- **Concept record:** user, profile, status (Generated / Approved / Deleted), origin (Topic / Idea / Revision), document as `jsonb`, tokens, cost, approved/deleted dates. Moves: Generated → Approved → Deleted → restored to Generated. Permanent delete only on deleted concepts.
- **Topic memory:** every suggested, generated, deleted or disliked topic is stored per profile with a fingerprint (normalized title keywords plus a hash). The prompt gets a short list (newest first, ≤120 lines / 3500 chars, deleted/disliked marked "✗"). Repeats are filtered server-side against the whole memory. Default reset clears suggested + disliked; `?all=true` clears everything.
- **Generation:** background job (one queue, sequential); HTTP returns immediately, the page polls. Budget check before every batch. Idea with count > 1 first asks the cheap model for N angles. Missing key / budget exceeded / unknown model stops the job; other errors skip that concept. Activity log: "12 konsept üretildi ($0.18) — süre …".
- **Fact checking:** on by default for history, sports, science and mystery profiles; web search on, sources required; a concept without sources gets a warning.
- **Word repetition check:** free local analysis first; only overused scenes go to the cheap model; a rewrite whose length changes by > 30% is rejected.
- **Revise loop:** a revised version is kept in memory for 2 hours until Seç or Sil; a discarded revision's cost is still added.
- **Prompts:** Turkish, embedded `.md` files with a small template renderer; system prompt cached.

**API routes**
- `GET api/concepts?status=`, `GET api/concepts/deleted`
- `GET|PUT|DELETE api/concepts/{id}`
- `POST api/concepts/{id}/approve`, `POST api/concepts/{id}/restore`, `DELETE api/concepts/{id}/purge`
- `POST api/concepts/{id}/revise`, `POST api/concepts/{id}/candidates/{candidateId}/accept`, `DELETE api/concepts/{id}/candidates/{candidateId}`
- `GET|PUT api/concepts/settings`
- `POST api/concepts/topics/suggest`, `POST api/concepts/topics/dislike`, `POST api/concepts/topics/generate`, `POST api/concepts/from-idea`
- `GET api/concepts/jobs/current`, `POST api/concepts/jobs/current/cancel`
- `POST api/concepts/topic-memory/reset[?all=true]`

**Web**
- `KonseptlerController` gained `KonuBegenme`, `Gorev`, `GorevIptal`, `AdayKabul`, `AdaySil` (candidate id in the query string). `SilinenlerController` unchanged (routes already matched).
- Konseptler page: model strip and model/effort modal, fine-tune modal (tone, audience, style, notes, fact checking), word-check switch, topic suggestions with 👎, generate-from-idea, progress bar with Stop, generated list (✏️ ✔ 🗑), scene editor with add/remove scene, revise modal (Seç / Sil / Bunu da düzenle).
- Silinenler: search, restore, permanent delete with confirm modal.
- `konseptler.css` is new. All calls go through `TK.api`, no `fetch`.

**Contracts:** `Hook` added to `ConceptDocument`; 18 new contract types in `Shared.Contracts/Concepts`.

**For the integrator**
- `GET api/concepts?status=Approved` returns `ConceptSummaryDto` (no document); Konseptten Video loads the full document from `GET api/concepts/{id}` or `IConceptReader`.
- Revise candidates and job state are held in memory; an API restart loses them.
- Model ids, prompt quality and Anthropic web search + structured output have not been seen working yet.
