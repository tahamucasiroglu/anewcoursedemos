import { defaultReportFilter, exportFileName, rangeDays, reportQuery, validateReportFilter, type ReportFilterValues } from "@okul/api-client";

// Spec 0017 AC-6 (istemci ön doğrulaması — sınırlar sunucuyla aynı): tarih zorunlu, başlangıç ≤ bitiş, uçlar dahil en fazla 366 gün, eşik
// 0–366; borç raporu tarihsiz; varsayılan bu ayın ilk günü – bugün; sorgu yalnızca raporun alanlarını taşır; dosya adı başlıktan okunur.
const values = (overrides: Partial<ReportFilterValues> = {}): ReportFilterValues => ({ ...defaultReportFilter("2026-11-20"), ...overrides });

it("varsayılan aralık bu ayın ilk günü – bugün", () => {
  expect(defaultReportFilter("2026-11-20")).toMatchObject({ startDate: "2026-11-01", endDate: "2026-11-20", branchId: "", overdueOnly: false });
});

it.each([
  ["2026-11-01", "2026-11-01", 1],
  ["2025-11-20", "2026-11-20", 366],
  ["2025-11-19", "2026-11-20", 367],
])("rangeDays(%s, %s) = %i (uçlar dahil)", (start, end, days) => {
  expect(rangeDays(start, end)).toBe(days);
});

it("tarih kuralları: eksik, ters, 367 gün hata; 366 gün geçerli; borç raporunda tarih yok", () => {
  expect(validateReportFilter("attendance", values({ startDate: "", endDate: "" }))).toEqual({ startDate: "required", endDate: "required" });
  expect(validateReportFilter("payments", values({ startDate: "2026-11-21" }))).toEqual({ endDate: "inverted" });
  expect(validateReportFilter("payments", values({ startDate: "2025-11-19" }))).toEqual({ endDate: "tooLong" });
  expect(validateReportFilter("payments", values({ startDate: "2025-11-20" }))).toEqual({});
  expect(validateReportFilter("balances", values({ startDate: "", endDate: "" }))).toEqual({});
});

it.each([
  ["", true],
  ["0", true],
  ["366", true],
  ["367", false],
  ["-1", false],
  ["2,5", false],
])("eşik %j geçerli mi: %s", (minAbsent, valid) => {
  expect(validateReportFilter("attendance", values({ minAbsent })).minAbsent === undefined).toBe(valid);
});

it("sorgu yalnızca raporun alanlarını taşır", () => {
  const all = values({ branchId: "b", classroomId: "c", minAbsent: "3", overdueOnly: true });
  expect(reportQuery("attendance", all)).toEqual({ startDate: "2026-11-01", endDate: "2026-11-20", branchId: "b", classroomId: "c", minAbsent: 3 });
  expect(reportQuery("payments", all)).toEqual({ startDate: "2026-11-01", endDate: "2026-11-20", branchId: "b" });
  expect(reportQuery("balances", all)).toEqual({ branchId: "b", overdueOnly: true });
});

it("dosya adı Content-Disposition'dan; yoksa yedek ad", () => {
  expect(exportFileName("attachment; filename=tahsilat-raporu-20.11.2026.csv; filename*=UTF-8''tahsilat-raporu-20.11.2026.csv", "x.csv")).toBe("tahsilat-raporu-20.11.2026.csv");
  expect(exportFileName('attachment; filename="borc-gecikme-raporu-20.11.2026.csv"', "x.csv")).toBe("borc-gecikme-raporu-20.11.2026.csv");
  expect(exportFileName(null, "yedek.csv")).toBe("yedek.csv");
});
