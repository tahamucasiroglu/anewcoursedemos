// Yönetici paneli testleri için sahte kayıtlar (spec 0017): yönetim testlerinin sahte API'si (adminFake — oturum, /api/me rolleri) üzerine
// üç kart, üç rapor sayfası ve sınıf listesi gövdeleri. Tutarlar sunucudaki gibi JSON sayısı.
export const branchId = "33333333-3333-4333-8333-333333333333";
export const classroomA = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";

export function attendanceCard(overrides: Record<string, unknown> = {}) {
  return {
    today: "2026-11-20",
    isSchoolDay: true,
    classroomCount: 3,
    takenCount: 2,
    notTakenCount: 1,
    notTaken: [{ classroomId: classroomA, branchName: "Merkez", classroomName: "5-B", teacherNames: ["Zeynep Öğretmen"], studentCount: 20, taken: false }],
    absentCount: 12,
    lateCount: 3,
    absentStudents: [{ studentId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1", fullName: "Ali Yılmaz", branchName: "Merkez", classroomName: "5-A" }],
    ...overrides,
  };
}

export function overdueCard(overrides: Record<string, unknown> = {}) {
  return {
    today: "2026-11-20",
    studentCount: 18,
    overdueAmount: 41500,
    top: [{ studentId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2", fullName: "Berk Demir", branchName: "Merkez", classroomName: "5-B", overdueAmount: 2333.33, oldestDue: "2026-10-01", daysOverdue: 50 }],
    ...overrides,
  };
}

export function unreadCard(overrides: Record<string, unknown> = {}) {
  return {
    since: "2026-11-14",
    announcementCount: 2,
    unreadGuardianCount: 9,
    top: [{ id: "cccccccc-cccc-4ccc-8ccc-ccccccccccc1", title: "Veli toplantısı", publishedAt: "2026-11-19T07:00:00Z", authorName: "Ayşe Kurucu", scope: "School", recipientCount: 12, readCount: 5 }],
    ...overrides,
  };
}

export function attendanceReport(overrides: Record<string, unknown> = {}) {
  return {
    items: [{ studentId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1", fullName: "Ali Yılmaz", schoolNumber: "125", branchName: "Merkez", classroomName: "5-A", absentDays: 4, lateDays: 1 }],
    page: 1,
    pageSize: 50,
    totalCount: 1,
    totals: { absentDays: 4, lateDays: 1 },
    ...overrides,
  };
}

export function paymentsReport(overrides: Record<string, unknown> = {}) {
  return {
    items: [
      { paymentId: "dddddddd-dddd-4ddd-8ddd-ddddddddddd1", date: "2026-11-05", receiptNo: 7, studentName: "Ali Yılmaz", branchName: "Merkez", amount: 1234.5, method: "Cash", cancelled: false, recordedByName: "Ayşe Kurucu" },
      { paymentId: "dddddddd-dddd-4ddd-8ddd-ddddddddddd2", date: "2026-11-06", receiptNo: 8, studentName: "Ali Yılmaz", branchName: "Merkez", amount: 99, method: "BankTransfer", cancelled: true, recordedByName: "Ayşe Kurucu" },
    ],
    page: 1,
    pageSize: 50,
    totalCount: 2,
    totals: {
      validAmount: 1234.5,
      validCount: 1,
      cancelledCount: 1,
      byMethod: [
        { method: "Cash", amount: 1234.5, count: 1 },
        { method: "BankTransfer", amount: 0, count: 0 },
        { method: "CardPos", amount: 0, count: 0 },
      ],
    },
    ...overrides,
  };
}

export function balancesReport(overrides: Record<string, unknown> = {}) {
  return {
    items: [{ studentId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2", fullName: "Berk Demir", branchName: "Merkez", classroomName: "5-B", planNames: "2026-2027 Eğitim Ücreti", net: 8500, paid: 1000, remaining: 7500, overdue: 2333.33, oldestOverdueDue: "2026-10-01" }],
    page: 1,
    pageSize: 50,
    totalCount: 1,
    totals: { net: 8500, paid: 1000, remaining: 7500, overdue: 2333.33 },
    today: "2026-11-20",
    ...overrides,
  };
}

export const classrooms = [
  { id: classroomA, termId: "88888888-8888-4888-8888-888888888881", branchId, branchName: "Merkez", gradeLevelId: "77777777-7777-4777-8777-777777777775", gradeLevelName: "5. sınıf", name: "5-A", isActive: true },
];
