import {
  addDays,
  assignmentBody,
  countSubmissions,
  gradesBody,
  invalidScores,
  parseDayInput,
  parseScore,
  scoreErrorStudents,
  submissionsBody,
  submissionsButtonText,
  submissionsResultText,
  validateAssessmentForm,
  validateAssignmentForm,
} from "@okul/api-client";

// Spec 0013 AC-6 (puan ayrıştırma, istemci katmanı — plan Risks 6) ve AC-1/AC-5 form kuralları: api-client'ın kendi test adımı
// olmadığı için kurallar mobilde sınanır. Sunucuyla aynı sınırlar: ders 60, başlık 150/100, açıklama 2000, teslim bugün … +90.
describe("academicsForms", () => {
  const today = "2026-11-20";

  it("puan: 0 ve 100 kabul, boş = girilmedi; -1, 101, ondalık ve harf geçersiz", () => {
    expect(parseScore("0")).toEqual({ ok: true, value: 0 });
    expect(parseScore("100")).toEqual({ ok: true, value: 100 });
    expect(parseScore(" 85 ")).toEqual({ ok: true, value: 85 });
    expect(parseScore("")).toEqual({ ok: true, value: null });
    for (const bad of ["-1", "101", "85.5", "85,5", "abc", "1e2", "+5"]) {
      expect(parseScore(bad)).toEqual({ ok: false });
    }

    expect(invalidScores({ a: "90", b: "101", c: "", d: "x" })).toEqual(["b", "d"]);
    expect(gradesBody({ a: "90", c: "" }, { a: "", c: "70" })).toEqual({ grades: [{ studentId: "a", score: 90 }, { studentId: "c", score: null }] });
  });

  // Review round 1, R1-3: eski ekrandan kayıt dokunulmamış öğrencinin puanını/teslimini silmesin — yalnızca ilk yüklemeden beri
  // değişen satırlar gider (sunucu gönderilmeyene dokunmaz); hiçbir şey değişmediyse gövde yok (istek gitmez).
  it("puan ve teslim gövdesi yalnızca değişen öğrencileri taşır; değişiklik yoksa null", () => {
    const initialScores = { a: "85", b: "", c: "70" };
    expect(gradesBody({ a: "85", b: "90", c: "70" }, initialScores)).toEqual({ grades: [{ studentId: "b", score: 90 }] });
    expect(gradesBody({ a: " 85 ", b: "", c: "70" }, initialScores)).toBeNull();
    expect(gradesBody({ a: "85", b: "", c: "" }, initialScores)).toEqual({ grades: [{ studentId: "c", score: null }] });

    const initialMarks = { a: "Unmarked", b: "Submitted", c: "NotSubmitted" } as const;
    expect(submissionsBody({ ...initialMarks, a: "Submitted" }, initialMarks)).toEqual({ entries: [{ studentId: "a", status: "Submitted" }] });
    expect(submissionsBody({ ...initialMarks }, initialMarks)).toBeNull();
  });

  it("sunucunun puan alanı hatasından öğrenci kimliği (plan Risks 6)", () => {
    expect(scoreErrorStudents([
      { property: "Grades[s2].Score" },
      { property: "Grades" },
      { property: "grades[s3].score" },
      { property: "Grades[s2].Score" },
    ])).toEqual(["s2", "s3"]);
    expect(scoreErrorStudents(undefined)).toEqual([]);
  });

  it("gün alanı GG.AA.YYYY; olmayan gün geçersiz; gün ekleme ay ve yıl geçer", () => {
    expect(parseDayInput("25.11.2026")).toBe("2026-11-25");
    expect(parseDayInput("5.1.2027")).toBe("2027-01-05");
    expect(parseDayInput("31.02.2027")).toBeNull();
    expect(parseDayInput("2026-11-25")).toBeNull();
    expect(addDays(today, 90)).toBe("2027-02-18");
    expect(addDays("2026-12-31", 1)).toBe("2027-01-01");
  });

  it("ödev formu: sınırda kabul, bir fazlası ve boş alan hata; teslim bugün ve 90. gün kabul, dün ve 91. gün hata", () => {
    const valid = { subject: "m".repeat(60), title: "b".repeat(150), description: "a".repeat(2000), dueDate: "20.11.2026" };
    expect(validateAssignmentForm(valid, today)).toEqual({});
    expect(validateAssignmentForm({ ...valid, dueDate: "18.02.2027" }, today)).toEqual({});
    expect(validateAssignmentForm({ subject: "m".repeat(61), title: " ", description: "a".repeat(2001), dueDate: "19.11.2026" }, today)).toEqual({
      subject: "tooLong",
      title: "required",
      description: "tooLong",
      dueDate: "dueOutOfRange",
    });
    expect(validateAssignmentForm({ ...valid, dueDate: "19.02.2027" }, today).dueDate).toBe("dueOutOfRange");
    expect(validateAssignmentForm({ ...valid, dueDate: "yarın" }, today).dueDate).toBe("invalidDate");
    expect(assignmentBody({ subject: " Matematik ", title: "Kesirler", description: "  ", dueDate: "25.11.2026" })).toEqual({
      subject: "Matematik",
      title: "Kesirler",
      description: null,
      dueDate: "2026-11-25",
    });
  });

  it("değerlendirme formu: başlık 100 sınırı, gelecek tarih hata, bugün kabul", () => {
    expect(validateAssessmentForm({ subject: "Türkçe", kind: "Written", title: "b".repeat(100), date: "20.11.2026" }, today)).toEqual({});
    expect(validateAssessmentForm({ subject: "", kind: "Oral", title: "b".repeat(101), date: "21.11.2026" }, today)).toEqual({ subject: "required", title: "tooLong", date: "future" });
  });

  it("teslim sayıları, 'Kaydet (3 etmedi)' ve sonuç metni", () => {
    const counts = countSubmissions({ a: "Submitted", b: "NotSubmitted", c: "NotSubmitted", d: "NotSubmitted", e: "Unmarked" });
    expect(counts).toEqual({ submitted: 1, notSubmitted: 3, unmarked: 1 });
    expect(submissionsButtonText(counts, { plain: "Kaydet", withNotSubmitted: "Kaydet ({n} etmedi)" })).toBe("Kaydet (3 etmedi)");
    expect(submissionsButtonText({ submitted: 2, notSubmitted: 0, unmarked: 0 }, { plain: "Kaydet", withNotSubmitted: "Kaydet ({n} etmedi)" })).toBe("Kaydet");
    expect(submissionsResultText({ submitted: 27, notSubmitted: 3, unmarked: 0, changed: 30 }, "Kaydedildi — {submitted} teslim etti, {notSubmitted} etmedi.")).toBe(
      "Kaydedildi — 27 teslim etti, 3 etmedi.",
    );
  });
});
