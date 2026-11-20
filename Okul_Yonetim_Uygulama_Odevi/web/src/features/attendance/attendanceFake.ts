// Yoklama ve bildirim ekranı testleri için sahte API (spec 0010): oturum (yenileme + /api/me — rol ve okunmamış sayısı
// verilebilir) hazır gelir; uçlar desenle eşlenir; gelen istekler (gövdeleriyle) kaydedilir. Ekranlar gerçek AuthSession /
// api-client kodunu kullanır, yalnızca ağ taklit edilir.
import { json, me, tokens } from "../auth/fakeApi";
import type { Route } from "../admin/adminFake";

export const classA = { id: "k1", name: "5-A", branchName: "Merkez", takenToday: false };
export const classB = { id: "k2", name: "6-A", branchName: "Avcılar", takenToday: true };

export function student(i: number, status = "Present") {
  return { studentId: `s${i}`, fullName: `Öğrenci ${String(i).padStart(2, "0")}`, status };
}

export function sheet(overrides: Record<string, unknown> = {}) {
  return { classroomId: "k1", classroomName: "5-A", branchName: "Merkez", day: "2026-11-20", isSchoolDay: true, isSaved: false, savedTime: null, students: [student(1), student(2)], ...overrides };
}

export function saveResult(overrides: Record<string, unknown> = {}) {
  return { present: 1, absent: 1, late: 0, notifiedGuardians: 2, studentsWithoutGuardian: 0, savedTime: "12:00", ...overrides };
}

export function attendanceFake(routes: Route[], meOverrides: Record<string, unknown> | (() => Record<string, unknown>) = { roles: ["Teacher"] }) {
  const requests: { method: string; path: string; body: unknown }[] = [];
  const fetch = async (request: Request): Promise<Response> => {
    const url = new URL(request.url, "http://x");
    const text = request.method === "PUT" || request.method === "POST" ? await request.clone().text() : "";
    requests.push({ method: request.method, path: url.pathname, body: text ? JSON.parse(text) : null });
    if (url.pathname === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (url.pathname === "/api/auth/refresh") {
      return json(200, tokens());
    }

    if (url.pathname === "/api/me") {
      return json(200, me(typeof meOverrides === "function" ? meOverrides() : meOverrides));
    }

    for (const [method, pattern, handler] of routes) {
      const match = url.pathname.match(pattern);
      if (method === request.method && match) {
        return handler(request, match);
      }
    }

    return json(404, { detail: "bulunamadı" });
  };
  return { fetch, requests };
}

export { json };
