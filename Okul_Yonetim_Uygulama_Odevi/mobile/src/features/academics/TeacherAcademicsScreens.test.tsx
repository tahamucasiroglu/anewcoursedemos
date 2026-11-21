import { fireEvent, render, screen, within } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";

// Spec 0013 AC-12 (5 dokunuş, onay yok) ve AC-13 (öğretmen ekranları, iki tema): "Ödev ve not" yalnızca öğretmende; sınıf seçimi
// (yükleniyor, boş, hata + tekrar dene, birden çok sınıf); Ödevler | Notlar sekmeleri; listeler (boş, dolu, sayılar); ödev ve
// değerlendirme formları (alan hataları — istek gitmez —, kaydediliyor-düğme kilitli, sunucu hatasında girilenler korunur,
// başarı); teslim listesi (kayan liste, Türkçe etiketler, seçili durum, kaydediliyor, hata + işaretler korunur, salt okunur,
// öğrenci yok); puan listesi (geçersiz puan istek göndermeden öğrencinin alanında, kaydedildi).
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const teacherMe = { fullName: "Ali Öğretmen", email: "ali@ornek.test", tenant: tenantA, roles: ["Teacher"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0 };
const classA = { id: "k1", name: "5-A", branchName: "Merkez", takenToday: false };
const classB = { id: "k2", name: "6-B", branchName: "Merkez", takenToday: false };
const homework = { id: "o1", subject: "Matematik", title: "Kesirler alıştırması", dueDate: "2026-11-25", createdByName: "Ali Öğretmen", submittedCount: 27, studentCount: 30 };
const exam = { id: "d1", subject: "Türkçe", kind: "Written", title: "1. yazılı", date: "2026-11-20", createdByName: "Ali Öğretmen", gradedCount: 2, studentCount: 3 };

function student(i: number, status = "Unmarked") {
  return { studentId: `s${i}`, fullName: `Öğrenci ${String(i).padStart(2, "0")}`, status };
}

function submissions(overrides: Record<string, unknown> = {}) {
  return { assignmentId: "o1", subject: "Matematik", title: "Kesirler alıştırması", dueDate: "2026-11-25", createdByName: "Ali Öğretmen", canEdit: true, students: [student(1), student(2)], ...overrides };
}

function gradesView(overrides: Record<string, unknown> = {}) {
  return {
    assessmentId: "d1",
    subject: "Türkçe",
    kind: "Written",
    title: "1. yazılı",
    date: "2026-11-20",
    createdByName: "Ali Öğretmen",
    canEdit: true,
    students: [
      { studentId: "s1", fullName: "Öğrenci 01", score: 85 },
      { studentId: "s2", fullName: "Öğrenci 02", score: null },
    ],
    ...overrides,
  };
}

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

/**
 * Sunucunun gerçek doğrulama yanıtı (T20, review round 1 R1-1; `AssessmentCreateTests` gerçek gövdeyi sınar): içerik türü
 * application/json, nArchitecture alan listesi `Errors: [{ Property, Errors }]`, `detail`'de hata ara katmanının kopyaladığı ilk
 * Türkçe alan mesajı.
 */
function validation(property: string, message: string): Response {
  const body = { Errors: [{ Property: property, Errors: [message] }], type: "https://example.com/probs/validation", title: "Validation error(s)", status: 400, detail: message };
  return new Response(JSON.stringify(body), { status: 400, headers: { "Content-Type": "application/json" } });
}

interface Handlers {
  roles?: string[];
  classrooms?: Handler;
  assignments?: Handler;
  createAssignment?: Handler;
  submissions?: Handler;
  saveSubmissions?: Handler;
  assessments?: Handler;
  createAssessment?: Handler;
  grades?: Handler;
  saveGrades?: Handler;
}

function fakeApi(handlers: Handlers) {
  const requests: { method: string; path: string; body: unknown }[] = [];
  globalThis.fetch = (async (request: Request) => {
    const path = new URL(request.url).pathname;
    const text = request.method === "PUT" || request.method === "POST" ? await request.clone().text() : "";
    requests.push({ method: request.method, path, body: text ? JSON.parse(text) : null });
    const route: [RegExp, string, Handler | undefined][] = [
      [/^\/api\/attendance\/classrooms$/, "GET", handlers.classrooms],
      [/^\/api\/academics\/classrooms\/[^/]+\/assignments$/, "GET", handlers.assignments],
      [/^\/api\/academics\/classrooms\/[^/]+\/assignments$/, "POST", handlers.createAssignment],
      [/^\/api\/academics\/assignments\/[^/]+\/submissions$/, "GET", handlers.submissions],
      [/^\/api\/academics\/assignments\/[^/]+\/submissions$/, "PUT", handlers.saveSubmissions],
      [/^\/api\/academics\/classrooms\/[^/]+\/assessments$/, "GET", handlers.assessments],
      [/^\/api\/academics\/classrooms\/[^/]+\/assessments$/, "POST", handlers.createAssessment],
      [/^\/api\/academics\/assessments\/[^/]+\/grades$/, "GET", handlers.grades],
      [/^\/api\/academics\/assessments\/[^/]+\/grades$/, "PUT", handlers.saveGrades],
    ];
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/login") {
      return json(200, tokens);
    }

    if (path === "/api/me") {
      return json(200, { ...teacherMe, roles: handlers.roles ?? teacherMe.roles });
    }

    const match = route.find(([pattern, method, handler]) => pattern.test(path) && method === request.method && handler);
    return match ? match[2]!(request) : json(404, {});
  }) as typeof fetch;
  return requests;
}

async function signIn() {
  await render(<App />);
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "ali@ornek.test");
  await fireEvent.changeText(screen.getByLabelText("Şifre"), "Sifre-1234");
  await fireEvent.press(screen.getByText("Giriş yap"));
  await screen.findByTestId("welcome");
}

async function openAcademics() {
  await fireEvent.press(screen.getByRole("button", { name: "Ödev ve not" }));
}

beforeEach(async () => {
  reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

describe.each(["light", "dark"] as const)("Ödev ve not (öğretmen) — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("40 öğrenci, tek sınıf: listeden ödeve dokunma + 3 'Teslim etmedi' + 'Kaydet' = 5 dokunuş, onay yok; sonuç duyurulur", async () => {
    const students = Array.from({ length: 40 }, (_, i) => student(i + 1));
    const requests = fakeApi({
      classrooms: () => json(200, [classA]),
      assignments: () => json(200, [homework]),
      submissions: () => json(200, submissions({ students })),
      saveSubmissions: () => json(200, { submitted: 37, notSubmitted: 3, unmarked: 0, changed: 40 }),
    });
    await signIn();
    await openAcademics();
    expect(await screen.findByRole("header", { name: "5-A" })).toBeTruthy();
    expect(screen.getByRole("tab", { name: "Ödevler" }).props.accessibilityState).toMatchObject({ selected: true });
    const row = await screen.findByTestId("assignment-row");
    expect(row.props.accessibilityLabel).toBe("Matematik — Kesirler alıştırması, Teslim 25.11.2026 · Ali Öğretmen, 27 / 30 teslim etti");
    let taps = 0;
    const tap = async (element: Parameters<typeof fireEvent.press>[0]) => {
      taps++;
      await fireEvent.press(element);
    };

    await tap(row);
    expect(await screen.findAllByTestId("submission-row")).toHaveLength(40);
    const list = screen.getByTestId("submissions-list");
    expect(list.type).toBe("RCTScrollView");
    expect(within(list).queryByRole("button", { name: "Kaydet" })).toBeNull();
    await tap(screen.getByRole("radio", { name: "Öğrenci 03: Teslim etmedi" }));
    await tap(screen.getByRole("radio", { name: "Öğrenci 17: Teslim etmedi" }));
    await tap(screen.getByRole("radio", { name: "Öğrenci 40: Teslim etmedi" }));
    expect(screen.getByRole("radio", { name: "Öğrenci 03: Teslim etmedi" }).props.accessibilityState).toMatchObject({ checked: true });
    expect(screen.getByRole("radio", { name: "Öğrenci 03: İşaretlenmedi" }).props.accessibilityState).toMatchObject({ checked: false });
    expect(screen.getByRole("radio", { name: "Öğrenci 03: Teslim etmedi" })).toHaveTextContent("✓ Teslim etmedi");
    await tap(screen.getByRole("button", { name: "Kaydet (3 etmedi)" }));

    expect(await screen.findByText("Kaydedildi — 37 teslim etti, 3 etmedi.")).toBeTruthy();
    expect(taps).toBe(5);
    const put = requests.find((r) => r.method === "PUT" && r.path.startsWith("/api/academics"));
    // Yalnızca değişen üç öğrenci gider (R1-3): dokunulmayan 37 öğrencinin kaydı sunucuda olduğu gibi kalır.
    expect(put?.body).toEqual({
      entries: [
        { studentId: "s3", status: "NotSubmitted" },
        { studentId: "s17", status: "NotSubmitted" },
        { studentId: "s40", status: "NotSubmitted" },
      ],
    });
    // 40 satır × 3 seçim ilk çizimde hazır (dokunuş sayısı kaydırmaya bağlı kalmaz); soğuk başlangıçta 5 sn sınırını aşabildiği için süre geniş.
  }, 20_000);

  it("teslim kaydı: kaydediliyor (düğme kilitli) → hata: işaretler korunur → tekrar Kaydet başarılı", async () => {
    let calls = 0;
    let fail: () => void = () => undefined;
    const failed = new Promise<Response>((resolve) => {
      fail = () => resolve(json(500, {}));
    });
    fakeApi({
      classrooms: () => json(200, [classA]),
      assignments: () => json(200, [homework]),
      submissions: () => json(200, submissions()),
      saveSubmissions: () => (++calls === 1 ? failed : json(200, { submitted: 1, notSubmitted: 1, unmarked: 0, changed: 2 })),
    });
    await signIn();
    await openAcademics();
    await fireEvent.press(await screen.findByTestId("assignment-row"));
    await fireEvent.press(await screen.findByRole("radio", { name: "Öğrenci 01: Teslim etti" }));
    await fireEvent.press(screen.getByRole("radio", { name: "Öğrenci 02: Teslim etmedi" }));
    await fireEvent.press(screen.getByRole("button", { name: "Kaydet (1 etmedi)" }));

    const saving = await screen.findByRole("button", { name: "Kaydediliyor…" });
    expect(saving.props.accessibilityState).toMatchObject({ disabled: true, busy: true });
    expect(screen.getByRole("radio", { name: "Öğrenci 01: Teslim etti" }).props.accessibilityState).toMatchObject({ disabled: true });
    fail();
    expect(await screen.findByText("Kaydedilemedi. Lütfen tekrar deneyin.")).toBeTruthy();
    expect(screen.getByRole("radio", { name: "Öğrenci 01: Teslim etti" }).props.accessibilityState).toMatchObject({ checked: true });
    expect(screen.getByRole("radio", { name: "Öğrenci 02: Teslim etmedi" }).props.accessibilityState).toMatchObject({ checked: true });

    await fireEvent.press(screen.getByRole("button", { name: "Kaydet (1 etmedi)" }));
    expect(await screen.findByText("Kaydedildi — 1 teslim etti, 1 etmedi.")).toBeTruthy();
  });

  it("yeni ödev: alan hataları istek göndermez; sunucu hatasında girilenler kalır; başarıda liste yenilenir", async () => {
    let listed = 0;
    let created = 0;
    const requests = fakeApi({
      classrooms: () => json(200, [classA]),
      assignments: () => json(200, ++listed === 1 ? [] : [homework]),
      createAssignment: () => (++created === 1 ? validation("DueDate", "Teslim tarihi bugün ile 90 gün sonrası arasında olmalı") : json(200, { id: "o1", studentCount: 30, notifiedGuardians: 28 })),
    });
    await signIn();
    await openAcademics();
    expect(await screen.findByText("Henüz ödev yok")).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Yeni ödev" }));
    expect(screen.getByLabelText("Teslim tarihi (GG.AA.YYYY)").props.value).toMatch(/^\d{2}\.\d{2}\.\d{4}$/);
    expect(screen.getByTestId("assignment-form-scroll").type).toBe("RCTScrollView");

    await fireEvent.changeText(screen.getByLabelText("Teslim tarihi (GG.AA.YYYY)"), "31.02.2027");
    await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
    expect(screen.getByText("Ders adı zorunludur")).toBeTruthy();
    expect(screen.getByText("Başlık zorunludur")).toBeTruthy();
    expect(screen.getByText("Tarihi GG.AA.YYYY biçiminde girin")).toBeTruthy();
    expect(requests.some((r) => r.method === "POST" && r.path.startsWith("/api/academics"))).toBe(false);

    await fireEvent.changeText(screen.getByLabelText("Ders"), "Matematik");
    await fireEvent.changeText(screen.getByLabelText("Başlık"), "Kesirler alıştırması");
    await fireEvent.press(screen.getByRole("button", { name: "Yarın" }));
    await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Teslim tarihi bugün ile 90 gün sonrası arasında olmalı");
    expect(screen.getByLabelText("Ders").props.value).toBe("Matematik");
    expect(screen.getByLabelText("Başlık").props.value).toBe("Kesirler alıştırması");

    await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
    expect((await screen.findByTestId("assignment-row")).props.accessibilityLabel).toContain("Matematik — Kesirler alıştırması");
    const post = requests.filter((r) => r.method === "POST" && r.path.startsWith("/api/academics"))[1];
    expect(post?.body).toMatchObject({ subject: "Matematik", title: "Kesirler alıştırması", description: null });
  });

  it("notlar: değerlendirme listesi ve puan listesi — geçersiz puan istek göndermez, öğrencinin alanında gösterilir; kaydedildi", async () => {
    const requests = fakeApi({
      classrooms: () => json(200, [classA]),
      assignments: () => json(200, []),
      assessments: () => json(200, [exam]),
      grades: () => json(200, gradesView()),
      saveGrades: () => json(200, { gradedCount: 2, studentCount: 2, changed: 1 }),
    });
    await signIn();
    await openAcademics();
    await fireEvent.press(await screen.findByRole("tab", { name: "Notlar" }));
    const row = await screen.findByTestId("assessment-row");
    expect(row.props.accessibilityLabel).toBe("Türkçe — 1. yazılı, Yazılı · 20.11.2026 · Ali Öğretmen, 2 / 3 puan girildi");
    await fireEvent.press(row);

    expect(screen.getByTestId("grades-list").type).toBe("RCTScrollView");
    expect((await screen.findByLabelText("Öğrenci 01 puanı")).props.value).toBe("85");
    await fireEvent.changeText(screen.getByLabelText("Öğrenci 02 puanı"), "101");
    await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
    expect(screen.getByText("Puan 0 ile 100 arasında tam sayı olmalı")).toBeTruthy();
    expect(screen.getByLabelText("Öğrenci 02 puanı").props.accessibilityHint).toBe("Puan 0 ile 100 arasında tam sayı olmalı");
    expect(requests.some((r) => r.method === "PUT" && r.path.startsWith("/api/academics"))).toBe(false);

    await fireEvent.changeText(screen.getByLabelText("Öğrenci 02 puanı"), "100");
    await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
    expect(await screen.findByText("Kaydedildi — 2 / 2 puan girildi.")).toBeTruthy();
    // Yalnızca değişen öğrenci gider (R1-3): Öğrenci 01'in yüklenen 85'i gönderilmez.
    expect(requests.find((r) => r.method === "PUT" && r.path.startsWith("/api/academics"))?.body).toEqual({ grades: [{ studentId: "s2", score: 100 }] });
  });
});

// Review round 1, R1-3: eski ekrandan kayıt, başka bir cihazdan girilmiş ve bu ekranda dokunulmamış öğrencinin puanını/teslimini
// silmemeli — istemci yalnızca ilk yüklemeden (ya da son başarılı kayıttan) beri değişen satırları gönderir; değişiklik yoksa istek
// gitmez, "Değişiklik yok" duyurulur.
it("puan listesi: değişiklik yoksa istek gitmez; kayıttan sonra yalnızca yeni değişiklik gider", async () => {
  const requests = fakeApi({
    classrooms: () => json(200, [classA]),
    assignments: () => json(200, []),
    assessments: () => json(200, [exam]),
    grades: () => json(200, gradesView()),
    saveGrades: () => json(200, { gradedCount: 2, studentCount: 2, changed: 1 }),
  });
  const puts = () => requests.filter((r) => r.method === "PUT" && r.path.startsWith("/api/academics")).map((r) => r.body);
  await signIn();
  await openAcademics();
  await fireEvent.press(await screen.findByRole("tab", { name: "Notlar" }));
  await fireEvent.press(await screen.findByTestId("assessment-row"));
  await screen.findByLabelText("Öğrenci 01 puanı");

  await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Değişiklik yok; kaydedilecek bir şey bulunmuyor.")).toBeTruthy();
  expect(puts()).toEqual([]);

  await fireEvent.changeText(screen.getByLabelText("Öğrenci 02 puanı"), "70");
  await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Kaydedildi — 2 / 2 puan girildi.")).toBeTruthy();
  await fireEvent.changeText(screen.getByLabelText("Öğrenci 01 puanı"), "");
  await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Kaydedildi — 2 / 2 puan girildi.")).toBeTruthy();
  expect(puts()).toEqual([{ grades: [{ studentId: "s2", score: 70 }] }, { grades: [{ studentId: "s1", score: null }] }]);
});

it("teslim listesi: değişiklik yoksa istek gitmez", async () => {
  const requests = fakeApi({
    classrooms: () => json(200, [classA]),
    assignments: () => json(200, [homework]),
    submissions: () => json(200, submissions({ students: [student(1, "Submitted"), student(2)] })),
    saveSubmissions: () => json(200, { submitted: 1, notSubmitted: 0, unmarked: 1, changed: 0 }),
  });
  await signIn();
  await openAcademics();
  await fireEvent.press(await screen.findByTestId("assignment-row"));
  await fireEvent.press(await screen.findByRole("radio", { name: "Öğrenci 02: Teslim etti" }));
  await fireEvent.press(screen.getByRole("radio", { name: "Öğrenci 02: İşaretlenmedi" }));
  await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));

  expect(await screen.findByText("Değişiklik yok; kaydedilecek bir şey bulunmuyor.")).toBeTruthy();
  expect(requests.some((r) => r.method === "PUT" && r.path.startsWith("/api/academics"))).toBe(false);
});

// Review round 1, R1-1 + plan Risks 6: sunucunun puan hatası (alan yolu `Grades[<öğrenci>].Score`) Türkçe duyurulur ve o öğrencinin
// alanı işaretlenir; puanlar korunur.
it("puan listesi: sunucunun puan hatası öğrencinin alanında ve Türkçe mesajla gösterilir", async () => {
  fakeApi({
    classrooms: () => json(200, [classA]),
    assignments: () => json(200, []),
    assessments: () => json(200, [exam]),
    grades: () => json(200, gradesView()),
    saveGrades: () => validation("Grades[s2].Score", "Puan 0 ile 100 arasında tam sayı olmalı"),
  });
  await signIn();
  await openAcademics();
  await fireEvent.press(await screen.findByRole("tab", { name: "Notlar" }));
  await fireEvent.press(await screen.findByTestId("assessment-row"));
  await fireEvent.changeText(await screen.findByLabelText("Öğrenci 02 puanı"), "90");
  await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));

  expect(await screen.findAllByText("Puan 0 ile 100 arasında tam sayı olmalı")).toHaveLength(2);
  expect(screen.getByLabelText("Öğrenci 02 puanı").props.accessibilityHint).toBe("Puan 0 ile 100 arasında tam sayı olmalı");
  expect(screen.getByLabelText("Öğrenci 01 puanı").props.accessibilityHint).toBeUndefined();
  expect(screen.getByLabelText("Öğrenci 02 puanı").props.value).toBe("90");
});

it("yeni değerlendirme: tür seçimi bildirilir, gelecek tarih alan hatası (istek yok), dönem dışı hata sunucudan duyurulur", async () => {
  let created = 0;
  const requests = fakeApi({
    classrooms: () => json(200, [classA]),
    assignments: () => json(200, []),
    assessments: () => json(200, []),
    createAssessment: () => (++created === 1 ? validation("Date", "Tarih sınıfın dönemi içinde olmalı") : json(200, { id: "d1", studentCount: 3 })),
  });
  await signIn();
  await openAcademics();
  await fireEvent.press(await screen.findByRole("tab", { name: "Notlar" }));
  expect(await screen.findByText("Henüz değerlendirme yok")).toBeTruthy();
  await fireEvent.press(screen.getByRole("button", { name: "Yeni değerlendirme" }));

  expect(screen.getByRole("radio", { name: "Yazılı" }).props.accessibilityState).toMatchObject({ checked: true });
  await fireEvent.press(screen.getByRole("radio", { name: "Sözlü" }));
  expect(screen.getByRole("radio", { name: "Sözlü" }).props.accessibilityState).toMatchObject({ checked: true });
  await fireEvent.changeText(screen.getByLabelText("Ders"), "Türkçe");
  await fireEvent.changeText(screen.getByLabelText("Başlık (ör. 1. yazılı)"), "Sözlü 1");
  await fireEvent.changeText(screen.getByLabelText("Tarih (GG.AA.YYYY)"), "01.01.2100");
  await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByText("Tarih bugünden sonra olamaz")).toBeTruthy();
  expect(requests.some((r) => r.method === "POST" && r.path.startsWith("/api/academics"))).toBe(false);

  await fireEvent.changeText(screen.getByLabelText("Tarih (GG.AA.YYYY)"), "01.06.2020");
  await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Tarih sınıfın dönemi içinde olmalı");
  expect(screen.getByLabelText("Başlık (ör. 1. yazılı)").props.value).toBe("Sözlü 1");
  expect(requests.find((r) => r.method === "POST" && r.path.startsWith("/api/academics"))?.body).toMatchObject({ subject: "Türkçe", kind: "Oral", title: "Sözlü 1", date: "2020-06-01" });
});

it("salt okunur: başka öğretmenin ödevi — düğme yok, seçimler kapalı, 'Yalnızca … değiştirebilir'; öğrencisiz ödev", async () => {
  let opened = 0;
  fakeApi({
    classrooms: () => json(200, [classA]),
    assignments: () => json(200, [homework, { ...homework, id: "o2", title: "Boş ödev" }]),
    submissions: () => json(200, ++opened === 1 ? submissions({ canEdit: false, createdByName: "Zeynep Hoca" }) : submissions({ students: [] })),
  });
  await signIn();
  await openAcademics();
  await fireEvent.press((await screen.findAllByTestId("assignment-row"))[0]);

  expect(await screen.findByText("Yalnızca Zeynep Hoca değiştirebilir")).toBeTruthy();
  expect(screen.queryByRole("button", { name: "Kaydet" })).toBeNull();
  expect(screen.getByRole("radio", { name: "Öğrenci 01: Teslim etti" }).props.accessibilityState).toMatchObject({ disabled: true });
  await fireEvent.press(screen.getByRole("button", { name: "Listeye dön" }));
  await fireEvent.press((await screen.findAllByTestId("assignment-row"))[1]);
  expect(await screen.findByText("Bu ödevde öğrenci yok")).toBeTruthy();
  expect(screen.queryByRole("button", { name: "Kaydet" })).toBeNull();
});

it("sınıf seçimi: hata + tekrar dene, birden çok sınıf listesi, boş liste", async () => {
  let calls = 0;
  fakeApi({ classrooms: () => (++calls === 1 ? json(500, {}) : json(200, [classA, classB])), assignments: () => json(200, []) });
  await signIn();
  await openAcademics();
  expect(await screen.findByText("Bilgiler alınamadı.")).toBeTruthy();
  await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(screen.getByTestId("academics-classrooms-list").type).toBe("RCTScrollView");
  await fireEvent.press(await screen.findByRole("button", { name: "6-B, Merkez, aç" }));
  expect(await screen.findByRole("header", { name: "6-B" })).toBeTruthy();
  await fireEvent.press(screen.getByRole("button", { name: "Sınıflara dön" }));
  expect(await screen.findByRole("button", { name: "5-A, Merkez, aç" })).toBeTruthy();
});

it("atanmış sınıf yok: 'Size atanmış sınıf yok'; öğretmen olmayan kişide 'Ödev ve not' yok", async () => {
  fakeApi({ classrooms: () => json(200, []) });
  await signIn();
  await openAcademics();
  expect(await screen.findByText("Size atanmış sınıf yok")).toBeTruthy();
});

it("veli rolünde öğretmen menüsü yok, veli menüsü var", async () => {
  const requests = fakeApi({ roles: ["Guardian"] });
  await signIn();
  expect(screen.queryByRole("button", { name: "Ödev ve not" })).toBeNull();
  expect(screen.getByRole("button", { name: "Ödev ve notlar" })).toBeTruthy();
  expect(requests.some((r) => r.path.startsWith("/api/academics"))).toBe(false);
});
