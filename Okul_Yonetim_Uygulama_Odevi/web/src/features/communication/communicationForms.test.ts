import { announcementBody, isDeadlineInRange, isQuarterHour, toggleDay, turkeyToday, validateAnnouncementForm, validateMessage, validateQuietHours } from "@okul/api-client";

// Spec 0011 AC-1, AC-5, AC-8, AC-9 (istemci kuralları, @okul/api-client/communication/communicationForms): sunucudaki
// sınırlarla aynı; hatalı alan varsa istek gönderilmez.
const base = { title: "Gezi", body: "Metin", scope: "Classroom" as const, scopeId: "k1", requestConsent: false, consentQuestion: "", consentDeadline: "" };

describe("duyuru formu", () => {
  it.each([
    [{ title: " " }, { title: "titleRequired" }],
    [{ title: "x".repeat(151) }, { title: "titleTooLong" }],
    [{ title: "x".repeat(150), body: "y".repeat(4000) }, {}],
    [{ body: "y".repeat(4001) }, { body: "bodyTooLong" }],
    [{ scopeId: "" }, { scopeId: "scopeRequired" }],
    [{ scope: "School" as const, scopeId: "" }, {}],
    [{ requestConsent: true, consentDeadline: "2026-11-27" }, { consentQuestion: "questionRequired" }],
    [{ requestConsent: true, consentQuestion: "s".repeat(301), consentDeadline: "2026-11-27" }, { consentQuestion: "questionTooLong" }],
    [{ requestConsent: true, consentQuestion: "Katılsın mı?" }, { consentDeadline: "deadlineRequired" }],
    [{ requestConsent: true, consentQuestion: "Katılsın mı?", consentDeadline: "2026-11-20" }, { consentDeadline: "deadlineRange" }],
  ])("%o → %o", (overrides, expected) => {
    expect(validateAnnouncementForm({ ...base, ...overrides }, "2026-11-20")).toEqual(expected);
  });

  it("son tarih yarından en geç 180 gün sonrasına", () => {
    expect(isDeadlineInRange("2026-11-21", "2026-11-20")).toBe(true);
    expect(isDeadlineInRange("2027-05-19", "2026-11-20")).toBe(true);
    expect(isDeadlineInRange("2027-05-20", "2026-11-20")).toBe(false);
    expect(isDeadlineInRange("2026-11-19", "2026-11-20")).toBe(false);
  });

  it("gövde kırpılır; onay istenmediyse soru ve son tarih gitmez; okulda kapsam kimliği yok", () => {
    expect(announcementBody({ ...base, title: " Gezi ", scope: "School", consentQuestion: "x", consentDeadline: "2026-11-27" })).toEqual({
      title: "Gezi", body: "Metin", scope: "School", scopeId: null, requestConsent: false, consentQuestion: null, consentDeadline: null,
    });
  });

  it("Türkiye günü UTC+3: UTC 20:59 aynı gün, 21:00 ertesi gün", () => {
    expect(turkeyToday(new Date("2026-11-20T20:59:00Z"))).toBe("2026-11-20");
    expect(turkeyToday(new Date("2026-11-20T21:00:00Z"))).toBe("2026-11-21");
  });
});

describe("mesaj ve mesai saatleri", () => {
  it("mesaj boş olamaz, en fazla 2000", () => {
    expect(validateMessage("  ")).toBe("messageRequired");
    expect(validateMessage("m".repeat(2001))).toBe("messageTooLong");
    expect(validateMessage("m".repeat(2000))).toBeUndefined();
  });

  it("mesai saatleri: 15 dk adımı, başlangıç bitişten önce, en az bir gün", () => {
    expect(isQuarterHour("08:15")).toBe(true);
    expect(isQuarterHour("08:10")).toBe(false);
    expect(validateQuietHours({ start: "17:00", end: "08:00", days: ["Monday"] })).toEqual({ start: "startBeforeEnd" });
    expect(validateQuietHours({ start: "08:10", end: "17:00", days: [] })).toEqual({ start: "startInvalid", days: "daysRequired" });
    expect(validateQuietHours({ start: "08:00", end: "17:00", days: ["Friday"] })).toEqual({});
    expect(toggleDay(["Friday"], "Monday")).toEqual(["Monday", "Friday"]);
    expect(toggleDay(["Monday", "Friday"], "Monday")).toEqual(["Friday"]);
  });
});
