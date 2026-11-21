import {
  availabilityBody,
  dayAndMonth,
  dayInputFromToday,
  groupByTurkeyDay,
  isAvailabilityTime,
  slotCountPreview,
  turkeyDayOf,
  turkeyTimeOf,
  validateAvailabilityForm,
  validateReason,
  validateTopic,
  weekdayOf,
} from "@okul/api-client";

// Spec 0016 AC-1, AC-5 ve AC-13 (istemci kuralları — api-client'ın kendi test adımı yok): sunucuyla aynı sınırlar — gün bugün … +60,
// saat 15 dk adımında (14:50 hata — plan A-2), başlangıç < bitiş, en fazla 8 saat, süre 15/20/30; konu zorunlu ≤ 300; neden ≤ 300.
// Dilim sayısı önizlemesi kısa kuyruğu saymaz; UTC anlar Türkiye saatiyle (sabit UTC+3) gün ve saate çevrilir.
describe("appointmentForms", () => {
  const today = "2026-11-20";
  const valid = { day: "26.11.2026", start: "14:00", end: "16:00", durationMinutes: 20 };

  it("geçerli form hata vermez; gövde günü yyyy-MM-dd taşır", () => {
    expect(validateAvailabilityForm(valid, today)).toEqual({});
    expect(availabilityBody(valid)).toEqual({ day: "2026-11-26", start: "14:00", end: "16:00", durationMinutes: 20 });
  });

  it.each([
    [{ day: "" }, { day: "dayRequired" }],
    [{ day: "2026-11-26" }, { day: "dayInvalid" }],
    [{ day: "31.02.2026" }, { day: "dayInvalid" }],
    [{ day: "19.11.2026" }, { day: "dayRange" }],
    [{ day: "20.01.2027" }, { day: "dayRange" }],
    [{ start: "14:10" }, { start: "startInvalid" }],
    [{ end: "14:50" }, { end: "endInvalid" }],
    [{ start: "2pm" }, { start: "startInvalid" }],
    [{ start: "16:00", end: "14:00" }, { start: "startBeforeEnd" }],
    [{ start: "14:00", end: "14:00" }, { start: "startBeforeEnd" }],
    [{ start: "08:00", end: "16:15" }, { end: "rangeTooLong" }],
    [{ durationMinutes: 25 }, { durationMinutes: "durationInvalid" }],
    [{ durationMinutes: null }, { durationMinutes: "durationInvalid" }],
  ])("geçersiz alan %j → %j", (patch, expected) => {
    expect(validateAvailabilityForm({ ...valid, ...patch }, today)).toEqual(expected);
  });

  it("bugün ve 60. gün kabul, 8 saat tam kabul", () => {
    expect(validateAvailabilityForm({ ...valid, day: "20.11.2026" }, today)).toEqual({});
    expect(validateAvailabilityForm({ ...valid, day: "19.01.2027" }, today)).toEqual({});
    expect(validateAvailabilityForm({ ...valid, start: "08:00", end: "16:00" }, today)).toEqual({});
    expect(dayInputFromToday(today, 0)).toBe("20.11.2026");
    expect(dayInputFromToday(today, 1)).toBe("21.11.2026");
  });

  it("dilim sayısı önizlemesi kısa kuyruğu saymaz", () => {
    expect(slotCountPreview("14:00", "16:00", 20)).toBe(6);
    expect(slotCountPreview("14:00", "14:45", 20)).toBe(2);
    expect(slotCountPreview("16:00", "14:00", 20)).toBe(0);
    expect(slotCountPreview("14:00", "16:00", null)).toBe(0);
    expect(isAvailabilityTime("09:45")).toBe(true);
    expect(isAvailabilityTime("24:00")).toBe(false);
  });

  it("konu ve neden sınırları", () => {
    expect(validateTopic("   ")).toBe("topicRequired");
    expect(validateTopic("k".repeat(301))).toBe("topicTooLong");
    expect(validateTopic(` ${"k".repeat(300)} `)).toBeUndefined();
    expect(validateReason("")).toBeUndefined();
    expect(validateReason("n".repeat(300))).toBeUndefined();
    expect(validateReason("n".repeat(301))).toBe("reasonTooLong");
  });

  it("UTC an Türkiye günü ve saatine çevrilir; gün gün gruplanır", () => {
    expect(turkeyTimeOf("2026-11-26T11:20:00Z")).toBe("14:20");
    expect(turkeyDayOf("2026-11-26T21:30:00Z")).toBe("2026-11-27");
    expect(weekdayOf("2026-11-26")).toBe("Thursday");
    expect(dayAndMonth("2026-11-26")).toEqual({ day: 26, month: 11 });
    const groups = groupByTurkeyDay(
      [{ at: "2026-11-25T07:00:00Z" }, { at: "2026-11-25T07:30:00Z" }, { at: "2026-11-26T11:00:00Z" }],
      (x) => x.at,
    );
    expect(groups.map((g) => [g.day, g.items.length])).toEqual([
      ["2026-11-25", 2],
      ["2026-11-26", 1],
    ]);
  });
});
