// Tahsilat ekranı testleri için sahte kayıtlar (spec 0015): yönetim testlerinin sahte API'si (adminFake — oturum, /api/me rolleri)
// üzerine tarife, indirim, cari listesi, öğrenci carisi ve makbuz gövdeleri. Tutarlar sunucudaki gibi JSON sayısı.
export const aliId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1";
export const planId = "cccccccc-cccc-4ccc-8ccc-ccccccccccc1";
export const paymentId = "dddddddd-dddd-4ddd-8ddd-ddddddddddd1";
export const feeId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1";
export const discountId = "ffffffff-ffff-4fff-8fff-fffffffffff1";
export const branchId = "33333333-3333-4333-8333-333333333333";

export const fee = { id: feeId, name: "2026-2027 Eğitim Ücreti", amount: 10000, isActive: true };
export const discount = { id: discountId, name: "Kardeş", percent: 15, isActive: true };

export function totals(net = 8500, paid = 1500, overdue = 0) {
  return { net, paid, remaining: net - paid, overdue };
}

export function accountsPage(items: unknown[] = [accountRow()], overrides: Record<string, unknown> = {}) {
  return { items, page: 1, pageSize: 25, totalCount: items.length, totals: totals(), branches: [{ id: branchId, name: "Merkez" }], ...overrides };
}

export function accountRow(overrides: Record<string, unknown> = {}) {
  return { studentId: aliId, fullName: "Ali Yılmaz", branchName: "Merkez", classroomName: "5-A", isActive: true, totals: totals(), ...overrides };
}

export function plan(overrides: Record<string, unknown> = {}) {
  return {
    id: planId,
    feeName: fee.name,
    feeAmount: 10000,
    discountName: "Kardeş",
    discountPercent: 15,
    discountAmount: 1500,
    installmentCount: 3,
    status: "Active",
    createdAt: "2026-11-20T09:00:00Z",
    cancelledAt: null,
    totals: totals(8500, 1500, 1333.33),
    installments: [
      { sequence: 1, dueDate: "2026-11-01", amount: 2833.33, paid: 1500, remaining: 1333.33, state: "Overdue" },
      { sequence: 2, dueDate: "2026-12-01", amount: 2833.33, paid: 0, remaining: 2833.33, state: "Pending" },
      { sequence: 3, dueDate: "2027-01-01", amount: 2833.34, paid: 0, remaining: 2833.34, state: "Pending" },
    ],
    ...overrides,
  };
}

export function paymentRow(overrides: Record<string, unknown> = {}) {
  return {
    id: paymentId,
    planId,
    planName: fee.name,
    receiptNo: 7,
    date: "2026-11-20",
    amount: 1500,
    method: "Cash",
    note: null,
    recordedByName: "Ayşe Kurucu",
    cancelled: false,
    cancelledAt: null,
    cancelReason: null,
    ...overrides,
  };
}

export function account(overrides: Record<string, unknown> = {}) {
  return {
    studentId: aliId,
    fullName: "Ali Yılmaz",
    branchName: "Merkez",
    classroomName: "5-A",
    isActive: true,
    totals: totals(8500, 1500, 1333.33),
    plans: [plan()],
    payments: [paymentRow()],
    movements: [
      { date: "2026-11-20", kind: "Plan", planName: fee.name, receiptNo: null, debit: 8500, credit: 0, balance: 8500 },
      { date: "2026-11-20", kind: "Payment", planName: fee.name, receiptNo: 7, debit: 0, credit: 1500, balance: 7000 },
    ],
    ...overrides,
  };
}

export function receipt(overrides: Record<string, unknown> = {}) {
  return {
    paymentId,
    receiptNo: 7,
    schoolName: "A Koleji",
    branchName: "Merkez",
    date: "2026-11-20",
    studentName: "Ali Yılmaz",
    planName: fee.name,
    amount: 3000,
    method: "CardPos",
    allocations: [
      { sequence: 1, dueDate: "2026-12-01", amount: 2833.33 },
      { sequence: 2, dueDate: "2027-01-01", amount: 166.67 },
    ],
    recordedByName: "Ayşe Kurucu",
    recordedAt: "2026-11-20T09:00:00Z",
    cancelled: false,
    cancelledAt: null,
    cancelReason: null,
    ...overrides,
  };
}
