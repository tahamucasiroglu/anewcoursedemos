// Öğrenci ekranı testleri için sahte kayıtlar (spec 0009); ağ taklidi yönetim testlerinin adminFake'iyle yapılır.
import { branchA, branchB, classroom } from "../admin/adminFake";

export const studentId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1";
export const siblingId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2";
export const guardianId = "cccccccc-cccc-4ccc-8ccc-ccccccccccc1";
export const classroomA = classroom({ id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaab1", name: "3-A" });

export function studentRow(overrides: Record<string, unknown> = {}) {
  return {
    id: studentId,
    fullName: "Zeynep Ak",
    schoolNumber: "301",
    branchId: branchA.id,
    branchName: branchA.name,
    classroomId: classroomA.id,
    classroomName: "3-A",
    guardianCount: 2,
    isActive: true,
    ...overrides,
  };
}

export function guardianView(overrides: Record<string, unknown> = {}) {
  return { userId: guardianId, fullName: "Ayla Ak", email: "ayla@ornek.test", phone: "+905321112233", relationship: "Mother", ...overrides };
}

export function studentDetail(overrides: Record<string, unknown> = {}) {
  return {
    id: studentId,
    fullName: "Zeynep Ak",
    birthDate: "2018-05-04",
    schoolNumber: "301",
    branchId: branchA.id,
    branchName: branchA.name,
    classroomId: classroomA.id,
    classroomName: "3-A",
    isActive: true,
    health: { available: true, healthNote: "Astım", allergy: "Fıstık" },
    guardians: [guardianView()],
    siblings: [{ id: siblingId, fullName: "Can Ak", branchName: branchB.name, classroomName: null, isActive: true }],
    pickupPersons: [{ fullName: "Hasan Ak", relationship: "Dede", phone: "+905320001122" }],
    ...overrides,
  };
}
