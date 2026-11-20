// Platform paneli testleri için sahte API (spec 0007): oturum (yenileme + /api/me) hazır gelir; platform uçları desenle
// eşlenir; gelen istekler kaydedilir. Ekranlar gerçek AuthSession / api-client kodunu kullanır.
import { json, me, tokens } from "../auth/fakeApi";

export type Route = [method: string, pattern: RegExp, handler: (request: Request) => Promise<Response> | Response];

export const schoolA = {
  tenantId: "77777777-7777-4777-8777-777777777777",
  name: "Çınar Koleji",
  createdAt: "2026-11-01T09:00:00Z",
  isActive: true,
  activeBranchCount: 2,
  activeMemberCount: 14,
  founders: [{ fullName: "Ayşe Kurucu", email: "ayse@ornek.test" }],
};

export const schoolB = { ...schoolA, tenantId: "88888888-8888-4888-8888-888888888888", name: "Işık Okulu", isActive: false, founders: [] };

export function schools(items: unknown[], total = items.length, page = 1) {
  return { items, total, page, pageSize: 25 };
}

export function platformFake(routes: Route[], isPlatformAdmin = true) {
  const requests: Request[] = [];
  const fetch = async (request: Request): Promise<Response> => {
    requests.push(request.clone());
    const url = new URL(request.url, "http://x");
    if (url.pathname === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (url.pathname === "/api/auth/refresh") {
      return json(200, tokens());
    }

    if (url.pathname === "/api/me") {
      return json(200, me({ isPlatformAdmin }));
    }

    for (const [method, pattern, handler] of routes) {
      if (method === request.method && pattern.test(url.pathname)) {
        return handler(request);
      }
    }

    return json(404, { detail: "bulunamadı" });
  };
  const sent = (method: string, path: RegExp) => requests.filter((r) => r.method === method && path.test(new URL(r.url, "http://x").pathname));
  return { fetch, requests, sent };
}

export { json };
