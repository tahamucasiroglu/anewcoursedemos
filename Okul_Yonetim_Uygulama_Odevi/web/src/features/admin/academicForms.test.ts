import { lengthInDays, nextSortOrder, validateCalendarEntryForm, validateClassroomForm, validateGradeLevelForm, validateTermForm } from "@okul/api-client";

// Spec 0008 AC-3, AC-4, AC-9 (istemci kuralları, @okul/api-client/admin/academicForms): sunucudaki kurallarla aynı sınırlar;
// hatalı alan varsa istek gönderilmez.
describe("seviye formu", () => {
  it.each([
    [{ name: "", sortOrder: "1" }, { name: "nameRequired" }],
    [{ name: "x".repeat(51), sortOrder: "1" }, { name: "nameTooLong" }],
    [{ name: "Hazırlık", sortOrder: "0" }, { sortOrder: "sortOrderRange" }],
    [{ name: "Hazırlık", sortOrder: "100" }, { sortOrder: "sortOrderRange" }],
    [{ name: "Hazırlık", sortOrder: "" }, { sortOrder: "sortOrderRange" }],
    [{ name: "Hazırlık", sortOrder: "2.5" }, { sortOrder: "sortOrderRange" }],
    [{ name: "x".repeat(50), sortOrder: "99" }, {}],
    [{ name: " Hazırlık ", sortOrder: "1" }, {}],
  ])("%o → %o", (values, expected) => {
    expect(validateGradeLevelForm(values)).toEqual(expected);
  });

  it("varsayılan sıra en büyük sıra + 1, en fazla 99", () => {
    expect(nextSortOrder([])).toBe(1);
    expect(nextSortOrder([{ sortOrder: 1 }, { sortOrder: 5 }])).toBe(6);
    expect(nextSortOrder([{ sortOrder: 99 }])).toBe(99);
  });
});

describe("dönem formu", () => {
  it("366 gün (uç günler dahil) kabul, 367 ret", () => {
    expect(lengthInDays("2026-09-01", "2027-09-01")).toBe(366);
    expect(validateTermForm({ name: "Güz", startDate: "2026-09-01", endDate: "2027-09-01" })).toEqual({});
    expect(validateTermForm({ name: "Güz", startDate: "2026-09-01", endDate: "2027-09-02" })).toEqual({ endDate: "termTooLong" });
  });

  it.each([
    [{ name: "", startDate: "2026-09-01", endDate: "2027-01-22" }, { name: "nameRequired" }],
    [{ name: "x".repeat(101), startDate: "2026-09-01", endDate: "2027-01-22" }, { name: "nameTooLong" }],
    [{ name: "Güz", startDate: "", endDate: "2027-01-22" }, { startDate: "datesRequired" }],
    [{ name: "Güz", startDate: "2026-09-01", endDate: "" }, { endDate: "datesRequired" }],
    [{ name: "Güz", startDate: "2026-09-01", endDate: "2026-09-01" }, { endDate: "termEndBeforeStart" }],
    [{ name: "Güz", startDate: "2026-09-01", endDate: "2026-08-01" }, { endDate: "termEndBeforeStart" }],
  ])("%o → %o", (values, expected) => {
    expect(validateTermForm(values)).toEqual(expected);
  });
});

describe("takvim kaydı formu", () => {
  const term = { startDate: "2026-09-01", endDate: "2027-01-22" };

  it("tek günlük kayıt ve dönemin uç günleri kabul", () => {
    expect(validateCalendarEntryForm({ title: "Bayram", kind: "Holiday", startDate: "2026-10-29", endDate: "2026-10-29" }, term)).toEqual({});
    expect(validateCalendarEntryForm({ title: "Dönem", kind: "Event", startDate: "2026-09-01", endDate: "2027-01-22" }, term)).toEqual({});
  });

  it.each([
    [{ title: "", kind: "Holiday", startDate: "2026-10-29", endDate: "2026-10-29" }, { title: "titleRequired" }],
    [{ title: "x".repeat(101), kind: "Holiday", startDate: "2026-10-29", endDate: "2026-10-29" }, { title: "titleTooLong" }],
    [{ title: "Parti", kind: "Party", startDate: "2026-10-29", endDate: "2026-10-29" }, { kind: "kindRequired" }],
    [{ title: "Türsüz", kind: "", startDate: "2026-10-29", endDate: "2026-10-29" }, { kind: "kindRequired" }],
    [{ title: "Ters", kind: "Exam", startDate: "2026-10-29", endDate: "2026-10-28" }, { endDate: "entryEndBeforeStart" }],
    [{ title: "Önce", kind: "Exam", startDate: "2026-08-31", endDate: "2026-09-02" }, { startDate: "entryOutsideTerm" }],
    [{ title: "Sonra", kind: "Exam", startDate: "2027-01-20", endDate: "2027-01-23" }, { endDate: "entryOutsideTerm" }],
    [{ title: "Boş", kind: "Exam", startDate: "", endDate: "" }, { startDate: "datesRequired", endDate: "datesRequired" }],
  ])("%o → %o", (values, expected) => {
    expect(validateCalendarEntryForm(values, term)).toEqual(expected);
  });
});

describe("sınıf formu", () => {
  it("ad zorunlu ve en fazla 30; şube (açarken) ve seviye seçimi zorunlu", () => {
    expect(validateClassroomForm({ branchId: "b", gradeLevelId: "g", name: "5-A" })).toEqual({});
    expect(validateClassroomForm({ branchId: "b", gradeLevelId: "g", name: "x".repeat(30) })).toEqual({});
    expect(validateClassroomForm({ branchId: "", gradeLevelId: "", name: " " })).toEqual({ branchId: "branchRequired", gradeLevelId: "levelRequired", name: "nameRequired" });
    expect(validateClassroomForm({ branchId: "b", gradeLevelId: "g", name: "x".repeat(31) })).toEqual({ name: "nameTooLong" });
    expect(validateClassroomForm({ branchId: "", gradeLevelId: "g", name: "5-A" }, false)).toEqual({});
  });
});
