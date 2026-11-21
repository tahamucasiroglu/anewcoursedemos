// Haftalık özet ekranı testleri için sahte veri (spec 0014): sınıf, öğrenci listesi ve öğrenci paneli kurucuları; ağ taklidi
// yoklamanın sahte API'sidir (oturum + /api/me hazır, uçlar desenle eşlenir, istekler gövdeleriyle kaydedilir).
export { attendanceFake as weeklyFake, json } from "../attendance/attendanceFake";

export const classA = { id: "k1", name: "5-A", branchName: "Merkez" };
export const classB = { id: "k2", name: "6-A", branchName: "Avcılar" };

export const teacherMe = { roles: ["Teacher"], teachesClassroom: true };

export function studentList(overrides: Record<string, unknown> = {}) {
  return {
    classroomId: "k1",
    classroomName: "5-A",
    weekStart: "2026-11-16",
    weekEnd: "2026-11-22",
    weekLabel: "16–22 Kasım",
    students: [
      { studentId: "s1", fullName: "Ali Yılmaz", status: "None" },
      { studentId: "s2", fullName: "Ayşe Kaya", status: "Draft" },
      { studentId: "s3", fullName: "Çınar Ak", status: "Sent" },
    ],
    ...overrides,
  };
}

export function basis(overrides: Record<string, unknown> = {}) {
  return {
    firstName: "Ali",
    weekStart: "2026-11-16",
    attendance: { daysTaken: 5, present: 3, absent: 1, late: 1, absentDays: ["2026-11-18"], lateDays: ["2026-11-19"] },
    assignments: [{ assignmentId: "a1", subject: "Matematik", title: "Kesirler", dueDate: "2026-11-20", status: "Submitted" }],
    grades: [{ assessmentId: "g1", subject: "Türkçe", kind: "Written", title: "1. yazılı", date: "2026-11-17", score: 85 }],
    teacherNote: null,
    ...overrides,
  };
}

export function panel(overrides: Record<string, unknown> = {}) {
  return {
    studentId: "s1",
    fullName: "Ali Yılmaz",
    weekStart: "2026-11-16",
    weekEnd: "2026-11-22",
    weekLabel: "16–22 Kasım",
    status: "None",
    basis: basis(),
    basisLine: "Dayanak: 4 gün geldi (1 gün geç), 1 gün gelmedi; 1 ödev; 1 not",
    hasData: true,
    draftId: null,
    draftText: null,
    finalText: null,
    sentAt: null,
    ...overrides,
  };
}

export function drafted(overrides: Record<string, unknown> = {}) {
  return panel({ status: "Draft", draftId: "d1", draftText: "Ali bu hafta dört gün okuldaydı.", ...overrides });
}

/** Gerçek doğrulama hatası gövdesi (conventions T20): Türkçe mesaj `detail`'de ve `Errors` dizisinde. */
export function validationProblem(property: string, message: string) {
  return { title: "Validation error(s)", status: 400, detail: message, Errors: [{ Property: property, Errors: [message] }] };
}
