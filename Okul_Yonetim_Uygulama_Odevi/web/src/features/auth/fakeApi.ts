// Testler için sahte API: istek adresine göre cevap verir ve gelen istekleri kaydeder (spec 0004 ekran testleri).
// Ekranlar gerçek AuthSession / api-client kodunu kullanır; yalnızca ağ taklit edilir.
export type Handler = (request: Request) => Promise<Response> | Response;

export function json(status: number, body: unknown): Response {
  return new Response(body === null ? null : JSON.stringify(body), {
    status,
    headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" },
  });
}

export const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
export const tenantB = { id: "22222222-2222-4222-8222-222222222222", name: "B Okulu" };

export function tokens(overrides: Partial<{ requiresTenantSelection: boolean; tenants: { id: string; name: string }[] }> = {}) {
  return {
    accessToken: "erisim-anahtari",
    accessTokenExpiresAt: "2026-11-20T10:15:00Z",
    refreshToken: null,
    requiresTenantSelection: false,
    tenants: [tenantA],
    ...overrides,
  };
}

export function me(overrides: Record<string, unknown> = {}) {
  return {
    fullName: "Ayşe Kurucu",
    email: "ayse@ornek.test",
    tenant: tenantA,
    roles: ["Founder"],
    branches: [{ id: "33333333-3333-4333-8333-333333333333", name: "Merkez" }],
    tenants: [tenantA],
    ...overrides,
  };
}

export function fakeApi(routes: Record<string, Handler>) {
  const requests: Request[] = [];
  const fetch = async (request: Request): Promise<Response> => {
    requests.push(request.clone());
    const path = new URL(request.url, "http://x").pathname;
    const route = routes[`${request.method} ${path}`];
    if (path === "/api/system-health" && !route) {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    return route ? route(request) : json(404, {});
  };
  return { fetch, requests, paths: () => requests.map((r) => `${r.method} ${new URL(r.url, "http://x").pathname}`) };
}
