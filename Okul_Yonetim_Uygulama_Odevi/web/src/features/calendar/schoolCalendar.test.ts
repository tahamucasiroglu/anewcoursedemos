import { calendarKindKey, dateRangeText, formatDay } from "@okul/api-client";

// Spec 0008 AC-10 (ortak yardımcılar, @okul/api-client/calendar/schoolCalendar — web ve mobil aynı metni kullanır):
// tek gün tek tarih, birden çok gün tarih aralığı; tür metin anahtarı.
it("gün.ay.yıl biçimi", () => {
  expect(formatDay("2026-10-29")).toBe("29.10.2026");
});

it("tek günlük kayıt tek tarih, birden çok gün aralık", () => {
  expect(dateRangeText("2026-10-29", "2026-10-29")).toBe("29.10.2026");
  expect(dateRangeText("2026-11-09", "2026-11-13")).toBe("09.11.2026 – 13.11.2026");
});

it("tür anahtarı; bilinmeyen tür 'other'", () => {
  expect(calendarKindKey("Holiday")).toBe("holiday");
  expect(calendarKindKey("Exam")).toBe("exam");
  expect(calendarKindKey("Event")).toBe("event");
  expect(calendarKindKey("Gezi")).toBe("other");
});
