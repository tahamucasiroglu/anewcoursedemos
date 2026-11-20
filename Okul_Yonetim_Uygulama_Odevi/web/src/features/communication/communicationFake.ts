// İletişim ekranı testleri için sahte API (spec 0011): oturum (yenileme + /api/me — rol verilebilir) hazır gelir; uçlar
// desenle eşlenir; gelen istekler (gövdeleriyle) kaydedilir. Ekranlar gerçek AuthSession / api-client kodunu kullanır.
import { attendanceFake, json } from "../attendance/attendanceFake";
import type { Route } from "../admin/adminFake";

export const announcementId = "abababab-abab-4bab-8bab-abababababa1";

export function row(overrides: Record<string, unknown> = {}) {
  return {
    id: announcementId,
    title: "Veli toplantısı",
    scope: "Classroom",
    authorName: "Ayşe Kurucu",
    publishedAt: "2026-11-20T09:00:00Z",
    requiresConsent: false,
    consentDeadline: null,
    recipientCount: 22,
    readCount: 18,
    ...overrides,
  };
}

export function detail(overrides: Record<string, unknown> = {}) {
  return {
    id: announcementId,
    title: "Müze gezisi",
    body: "Pazartesi müze gezisi var.",
    scope: "Classroom",
    authorName: "Ayşe Kurucu",
    publishedAt: "2026-11-20T09:00:00Z",
    consentQuestion: "Geziye katılsın mı?",
    consentDeadline: "2026-11-27",
    consentOpen: true,
    children: [],
    ...overrides,
  };
}

export function results(overrides: Record<string, unknown> = {}) {
  return {
    id: announcementId,
    recipientCount: 22,
    readCount: 18,
    unread: [{ guardianName: "Veli Kaya", childNames: ["Ayşe Kaya"] }],
    consent: {
      approved: 15,
      declined: 2,
      pending: 5,
      students: [
        { studentId: "s1", studentName: "Ali Yılmaz", answer: "Approved", answeredByName: "Veli Yılmaz", answeredAt: "2026-11-20T10:00:00Z" },
        { studentId: "s2", studentName: "Ayşe Kaya", answer: null, answeredByName: null, answeredAt: null },
      ],
    },
    ...overrides,
  };
}

export const classrooms = [{ id: "k1", termId: "t1", branchId: "b1", branchName: "Merkez", gradeLevelId: "g1", gradeLevelName: "5. sınıf", name: "5-A", isActive: true }];
export const myClassrooms = [{ id: "k1", name: "5-A", branchName: "Merkez", takenToday: false }];

/** Liste, ayrıntı ve sonuç uçları hazır; ek uçlar önce eşlenir. */
export function communicationFake(extra: Route[] = [], roles = ["Founder"]) {
  return attendanceFake([
    ...extra,
    ["GET", /^\/api\/announcements$/, () => json(200, [row()])],
    ["GET", /^\/api\/announcements\/[^/]+\/results$/, () => json(200, results())],
    ["GET", /^\/api\/announcements\/[^/]+$/, () => json(200, detail())],
    ["GET", /^\/api\/classrooms$/, () => json(200, classrooms)],
    ["GET", /^\/api\/attendance\/classrooms$/, () => json(200, myClassrooms)],
  ], { roles });
}

export { json };
