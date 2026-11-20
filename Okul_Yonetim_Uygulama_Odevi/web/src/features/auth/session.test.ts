import { AuthSession, type RefreshTokenStore } from "@okul/api-client";
import { fakeApi, json, me, tokens } from "./fakeApi";

// Ortak oturum kodu (packages/api-client, spec 0004 AC-8): 401'de tek yenileme + tekrar; aynı anda gelen 401'ler tek
// yenilemeyi paylaşır; yenileme başarısızsa onExpired. Mobil kipinde yenileme anahtarı gövdede ve depoda.
afterEach(() => {
  vi.unstubAllGlobals();
});

function memoryStore(initial: string | null = null): RefreshTokenStore & { value: string | null } {
  const store = {
    value: initial,
    get: async () => store.value,
    set: async (token: string | null) => {
      store.value = token;
    },
  };
  return store;
}

it("401 alan istek bir kez yenilenip yeni anahtarla tekrarlanır", async () => {
  let current = "eski";
  const api = fakeApi({
    "POST /api/auth/login": () => json(200, tokens({ refreshToken: null } as never)),
    "POST /api/auth/refresh": () => {
      current = "yeni";
      return json(200, { ...tokens(), accessToken: "yeni" });
    },
    "GET /api/me": (request) =>
      request.headers.get("Authorization") === `Bearer ${current}` && current === "yeni" ? json(200, me()) : json(401, {}),
  });
  vi.stubGlobal("fetch", api.fetch);
  const session = new AuthSession({ baseUrl: "http://localhost:5080", delivery: "cookie" });
  await session.login("a@ornek.test", "Gizli-Sifre-1");

  const result = await session.me();

  expect(result?.fullName).toBe("Ayşe Kurucu");
  expect(api.paths()).toEqual(["POST /api/auth/login", "GET /api/me", "POST /api/auth/refresh", "GET /api/me"]);
});

it("aynı anda gelen 401'ler tek yenilemeyi paylaşır; yenileme başarısızsa onExpired bir kez çağrılır", async () => {
  const onExpired = vi.fn();
  const api = fakeApi({
    "POST /api/auth/login": () => json(200, tokens()),
    "POST /api/auth/refresh": () => json(401, {}),
    "GET /api/me": () => json(401, {}),
  });
  vi.stubGlobal("fetch", api.fetch);
  const session = new AuthSession({ baseUrl: "http://localhost:5080", delivery: "cookie", onExpired });
  await session.login("a@ornek.test", "Gizli-Sifre-1");

  await Promise.all([session.me(), session.me(), session.me()]);

  expect(api.paths().filter((p) => p === "POST /api/auth/refresh")).toHaveLength(1);
  expect(onExpired).toHaveBeenCalledTimes(1);
  expect(session.isSignedIn).toBe(false);
});

it("mobil kipinde yenileme anahtarı gövdede istenir, depoya yazılır, çıkışta silinir", async () => {
  const store = memoryStore();
  const api = fakeApi({
    "POST /api/auth/login": () => json(200, { ...tokens(), refreshToken: "yenileme-1" }),
    "POST /api/auth/refresh": () => json(200, { ...tokens(), refreshToken: "yenileme-2" }),
    "POST /api/auth/logout": () => new Response(null, { status: 204 }),
  });
  vi.stubGlobal("fetch", api.fetch);
  const session = new AuthSession({ baseUrl: "http://localhost:5080", delivery: "body", store });

  await session.login("a@ornek.test", "Gizli-Sifre-1");
  expect(await api.requests[0].json()).toMatchObject({ refreshTokenInBody: true });
  expect(store.value).toBe("yenileme-1");
  await session.resume();
  expect(await api.requests[1].json()).toEqual({ refreshToken: "yenileme-1" });
  expect(store.value).toBe("yenileme-2");
  await session.logout();

  expect(store.value).toBeNull();
  expect(session.isSignedIn).toBe(false);
});

it("mobil kipinde depoda anahtar yoksa açılışta sunucuya gitmeden 'oturum yok' döner", async () => {
  const api = fakeApi({});
  vi.stubGlobal("fetch", api.fetch);
  const session = new AuthSession({ baseUrl: "http://localhost:5080", delivery: "body", store: memoryStore() });

  const outcome = await session.resume();

  expect(outcome.kind).toBe("rejected");
  expect(api.requests).toHaveLength(0);
});

it("yenileme anında sunucuya ulaşılamazsa oturum bitmiş sayılmaz: anahtar silinmez, onExpired çağrılmaz (review L-2)", async () => {
  const onExpired = vi.fn();
  const store = memoryStore("kayitli");
  const api = fakeApi({
    "POST /api/auth/refresh": () => {
      throw new TypeError("Failed to fetch");
    },
    "POST /api/auth/login": () => json(200, { ...tokens(), refreshToken: "kayitli" }),
    "GET /api/me": () => json(401, {}),
  });
  vi.stubGlobal("fetch", api.fetch);
  const session = new AuthSession({ baseUrl: "http://localhost:5080", delivery: "body", store, onExpired });
  await session.login("a@ornek.test", "Gizli-Sifre-1");

  await expect(session.me()).rejects.toThrow();

  expect(onExpired).not.toHaveBeenCalled();
  expect(store.value).toBe("kayitli");
});

it("yenileme ağ geçidinden 502 alırsa oturum bitmiş sayılmaz (Pi'de API yeniden başlarken; review L-6)", async () => {
  const onExpired = vi.fn();
  const store = memoryStore("kayitli");
  const api = fakeApi({
    "POST /api/auth/login": () => json(200, { ...tokens(), refreshToken: "kayitli" }),
    "POST /api/auth/refresh": () => new Response("<html>502 Bad Gateway</html>", { status: 502 }),
    "GET /api/me": () => json(401, {}),
  });
  vi.stubGlobal("fetch", api.fetch);
  const session = new AuthSession({ baseUrl: "http://localhost:5080", delivery: "body", store, onExpired });
  await session.login("a@ornek.test", "Gizli-Sifre-1");

  await expect(session.me()).rejects.toThrow();
  expect((await session.resume()).kind).toBe("unreachable");

  expect(onExpired).not.toHaveBeenCalled();
  expect(store.value).toBe("kayitli");
});
