// @vitest-environment node
import { AuthSession, uploadVoiceNote } from "@okul/api-client";
import { fakeApi, json, tokens } from "./fakeApi";

// Spec 0012 review round 1 (L-4): sesli not yüklemesi (çok parçalı form, packages/api-client voiceNote.ts) 401 alırsa oturum
// bir kez yenilenir ve istek yeni anahtarla, iki form alanıyla (conversationId, audio) aynen tekrarlanır. Ortam node:
// FormData/Blob/Request aynı (undici) uygulamadan gelsin diye.
afterEach(() => {
  vi.unstubAllGlobals();
});

it("ses yüklemesi 401 → yenile → yeni anahtarla iki form alanıyla tekrar; taslak döner", async () => {
  let current = "erisim-anahtari";
  const draft = { draftId: "d1", transcript: "döküm", draft: "taslak", durationSeconds: 12 };
  const api = fakeApi({
    "POST /api/auth/login": () => json(200, tokens()),
    "POST /api/auth/refresh": () => {
      current = "yeni-anahtar";
      return json(200, { ...tokens(), accessToken: "yeni-anahtar" });
    },
    "POST /api/ai/voice-message-drafts": (request) =>
      current === "yeni-anahtar" && request.headers.get("Authorization") === "Bearer yeni-anahtar" ? json(200, draft) : json(401, {}),
  });
  vi.stubGlobal("fetch", api.fetch);
  const session = new AuthSession({ baseUrl: "http://localhost:5080", delivery: "cookie" });
  await session.login("ogretmen@ornek.test", "Gizli-Sifre-1");
  const audio = new Blob([new Uint8Array([1, 2, 3, 4, 5])], { type: "audio/webm" });

  const outcome = await uploadVoiceNote(session.client, "k1", audio);

  expect(outcome).toEqual({ ok: true, data: draft });
  expect(api.paths()).toEqual(["POST /api/auth/login", "POST /api/ai/voice-message-drafts", "POST /api/auth/refresh", "POST /api/ai/voice-message-drafts"]);
  const [first, retried] = api.requests.filter((r) => new URL(r.url).pathname === "/api/ai/voice-message-drafts");
  expect(first.headers.get("Authorization")).toBe("Bearer erisim-anahtari");
  expect(retried.headers.get("Authorization")).toBe("Bearer yeni-anahtar");
  for (const request of [first, retried]) {
    const form = await request.formData();
    expect([...form.keys()].sort()).toEqual(["audio", "conversationId"]);
    expect(form.get("conversationId")).toBe("k1");
    const file = form.get("audio") as File;
    expect(file.name).toBe("sesli-not.webm");
    expect(file.type).toBe("audio/webm");
    expect(new Uint8Array(await file.arrayBuffer())).toEqual(new Uint8Array([1, 2, 3, 4, 5]));
  }
});
