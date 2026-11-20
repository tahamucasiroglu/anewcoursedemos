// Yönetim ekranı testleri için sahte API (spec 0005): oturum (yenileme + /api/me) hazır gelir; yönetim uçları desenle
// eşlenir; gelen istekler kaydedilir. Ekranlar gerçek AuthSession / api-client kodunu kullanır, yalnızca ağ taklit edilir.
import { json, me, tokens } from "../auth/fakeApi";

export type Route = [method: string, pattern: RegExp, handler: (request: Request, match: RegExpMatchArray) => Promise<Response> | Response];

type FakeBranch = { id: string; name: string; address: string | null; phone: string | null; isActive: boolean };
export const branchA: FakeBranch = { id: "33333333-3333-4333-8333-333333333333", name: "Merkez", address: null, phone: null, isActive: true };
export const branchB: FakeBranch = { id: "44444444-4444-4444-8444-444444444444", name: "Avcılar", address: "Avcılar Mh.", phone: "+902125554433", isActive: true };
export const founderId = "55555555-5555-4555-8555-555555555555";
export const teacherId = "66666666-6666-4666-8666-666666666666";

// Akademik yapı (spec 0008) sahte kayıtları.
export const levelPrep = { id: "77777777-7777-4777-8777-777777777771", name: "Hazırlık", sortOrder: 1, isActive: true };
export const levelFifth = { id: "77777777-7777-4777-8777-777777777775", name: "5. sınıf", sortOrder: 5, isActive: true };
export const termAutumn = { id: "88888888-8888-4888-8888-888888888881", name: "2026-2027 Güz", startDate: "2026-09-01", endDate: "2027-01-22", isCurrent: true, isUpcoming: false };
export const termSpring = { id: "88888888-8888-4888-8888-888888888882", name: "2026-2027 Bahar", startDate: "2027-02-08", endDate: "2027-06-18", isCurrent: false, isUpcoming: false };
export const holiday = { id: "99999999-9999-4999-8999-999999999991", termId: termAutumn.id, title: "Cumhuriyet Bayramı", kind: "Holiday", startDate: "2026-10-29", endDate: "2026-10-29" };
export const examWeek = { id: "99999999-9999-4999-8999-999999999992", termId: termAutumn.id, title: "Ara sınav haftası", kind: "Exam", startDate: "2026-11-09", endDate: "2026-11-13" };

export function classroom(overrides: Record<string, unknown> = {}) {
  return {
    id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1",
    termId: termAutumn.id,
    branchId: branchA.id,
    branchName: branchA.name,
    branchIsActive: true,
    gradeLevelId: levelFifth.id,
    gradeLevelName: levelFifth.name,
    gradeLevelSortOrder: 5,
    name: "5-A",
    isActive: true,
    ...overrides,
  };
}

export function memberRow(overrides: Record<string, unknown> = {}) {
  return {
    userId: teacherId,
    fullName: "Ali Öğretmen",
    email: "ali@ornek.test",
    phone: null,
    isActive: true,
    roles: [{ role: "Teacher", branchIds: [branchA.id] }],
    ...overrides,
  };
}

export function memberDetail(overrides: Record<string, unknown> = {}) {
  return { ...memberRow(), isSelf: false, canResetPassword: true, canEditProfile: true, ...overrides };
}

export function page(items: unknown[], total = items.length, pageNo = 1) {
  return { items, total, page: pageNo, pageSize: 25 };
}

export function adminFake(routes: Route[], roles: string[] = ["Founder"]) {
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
      return json(200, me({ roles }));
    }

    for (const [method, pattern, handler] of routes) {
      const match = url.pathname.match(pattern);
      if (method === request.method && match) {
        return handler(request, match);
      }
    }

    return json(404, { detail: "bulunamadı" });
  };
  const sent = (method: string, path: RegExp) => requests.filter((r) => r.method === method && path.test(new URL(r.url, "http://x").pathname));
  return { fetch, requests, sent };
}

export { json };
