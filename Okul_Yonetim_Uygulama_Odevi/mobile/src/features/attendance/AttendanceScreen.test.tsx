import { fireEvent, render, screen } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";

// Spec 0010 AC-5 (5 dokunuş, onay yok) ve AC-13 (mobil): "Yoklama al" yalnızca öğretmende; tek sınıfta liste doğrudan
// açılır; herkes "Var", düğmede sayılar; kaydediliyor (düğme kilitli) → sonuç; kaydedildi saati; okul günü değil; öğrenci
// yok (kaydet kapalı); hata (işaretler korunur, tekrar Kaydet); erişim yok; durum düğmesi etiketleri öğrenci adını içerir ve
// seçili durum bildirilir — iki temada.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const teacherMe = { fullName: "Ali Öğretmen", email: "ali@ornek.test", tenant: tenantA, roles: ["Teacher"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0 };
const classA = { id: "k1", name: "5-A", branchName: "Merkez", takenToday: false };

function student(i: number, status = "Present") {
  return { studentId: `s${i}`, fullName: `Öğrenci ${String(i).padStart(2, "0")}`, status };
}

function sheet(overrides: Record<string, unknown> = {}) {
  return { classroomId: "k1", classroomName: "5-A", branchName: "Merkez", day: "2026-11-20", isSchoolDay: true, isSaved: false, savedTime: null, students: [student(1), student(2)], ...overrides };
}

function result(overrides: Record<string, unknown> = {}) {
  return { present: 37, absent: 3, late: 0, notifiedGuardians: 3, studentsWithoutGuardian: 0, savedTime: "12:00", ...overrides };
}

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

function fakeApi(handlers: { classrooms?: Handler; today?: Handler; save?: Handler; roles?: string[] }) {
  const requests: { method: string; path: string; body: unknown }[] = [];
  globalThis.fetch = (async (request: Request) => {
    const path = new URL(request.url).pathname;
    const text = request.method === "PUT" ? await request.clone().text() : "";
    requests.push({ method: request.method, path, body: text ? JSON.parse(text) : null });
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/login") {
      return json(200, tokens);
    }

    if (path === "/api/me") {
      return json(200, { ...teacherMe, roles: handlers.roles ?? teacherMe.roles });
    }

    if (path === "/api/attendance/classrooms" && handlers.classrooms) {
      return handlers.classrooms(request);
    }

    if (/^\/api\/attendance\/classrooms\/[^/]+\/today$/.test(path)) {
      const handler = request.method === "PUT" ? handlers.save : handlers.today;
      if (handler) {
        return handler(request);
      }
    }

    return json(404, {});
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

beforeEach(async () => {
  reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

describe.each(["light", "dark"] as const)("Yoklama — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("40 öğrenci, tek sınıf: 'Yoklama al' + 3 'Yok' + 'Kaydet' = 5 dokunuş, onay yok; sonuç duyurulur", async () => {
    const students = Array.from({ length: 40 }, (_, i) => student(i + 1));
    const requests = fakeApi({
      classrooms: () => json(200, [classA]),
      today: () => json(200, sheet({ students })),
      save: () => json(200, result()),
    });
    await signIn();
    let taps = 0;
    const tap = async (element: Parameters<typeof fireEvent.press>[0]) => {
      taps++;
      await fireEvent.press(element);
    };

    await tap(screen.getByRole("button", { name: "Yoklama al" }));
    expect(await screen.findByRole("header", { name: "5-A" })).toBeTruthy();
    expect(screen.getAllByTestId("attendance-row")).toHaveLength(40);
    expect(screen.getByRole("button", { name: "Kaydet" })).toBeTruthy();
    await tap(screen.getByRole("radio", { name: "Öğrenci 03: Yok" }));
    await tap(screen.getByRole("radio", { name: "Öğrenci 17: Yok" }));
    await tap(screen.getByRole("radio", { name: "Öğrenci 40: Yok" }));
    expect(screen.getByRole("radio", { name: "Öğrenci 03: Yok" }).props.accessibilityState).toMatchObject({ checked: true });
    expect(screen.getByRole("radio", { name: "Öğrenci 03: Var" }).props.accessibilityState).toMatchObject({ checked: false });
    await tap(screen.getByRole("button", { name: "Kaydet (3 yok)" }));

    expect(await screen.findByText("Kaydedildi — 37 var, 3 yok, 0 geç. 3 veliye bildirim gitti.")).toBeTruthy();
    expect(taps).toBe(5);
    expect(screen.getByTestId("attendance-saved")).toHaveTextContent("Kaydedildi, saat 12:00");
    const put = requests.find((r) => r.method === "PUT");
    expect(put?.body).toMatchObject({ day: "2026-11-20" });
    const entries = (put?.body as { entries: { studentId: string; status: string }[] }).entries;
    expect(entries).toHaveLength(40);
    expect(entries.filter((e) => e.status === "Absent").map((e) => e.studentId)).toEqual(["s3", "s17", "s40"]);
  });

  it("kaydediliyor: düğme kilitli → hata: işaretler korunur → tekrar Kaydet başarılı; velisi olmayan sayısı", async () => {
    let calls = 0;
    let fail: () => void = () => undefined;
    fakeApi({
      classrooms: () => json(200, [classA]),
      today: () => json(200, sheet()),
      save: () => (++calls === 1 ? new Promise<Response>((resolve) => { fail = () => resolve(json(500, {})); }) : json(200, result({ present: 0, absent: 1, late: 1, notifiedGuardians: 0, studentsWithoutGuardian: 1 }))),
    });
    await signIn();
    await fireEvent.press(screen.getByRole("button", { name: "Yoklama al" }));
    await fireEvent.press(await screen.findByRole("radio", { name: "Öğrenci 01: Yok" }));
    await fireEvent.press(screen.getByRole("radio", { name: "Öğrenci 02: Geç" }));
    await fireEvent.press(screen.getByRole("button", { name: "Kaydet (1 yok, 1 geç)" }));

    const saving = await screen.findByRole("button", { name: "Kaydediliyor…" });
    expect(saving.props.accessibilityState).toMatchObject({ disabled: true, busy: true });
    expect(screen.getByRole("radio", { name: "Öğrenci 01: Var" }).props.accessibilityState).toMatchObject({ disabled: true });
    fail();
    expect(await screen.findByText("Kaydedilemedi; işaretleriniz korunuyor. Tekrar deneyin.")).toBeTruthy();
    expect(screen.getByRole("radio", { name: "Öğrenci 01: Yok" }).props.accessibilityState).toMatchObject({ checked: true });
    expect(screen.getByRole("radio", { name: "Öğrenci 02: Geç" }).props.accessibilityState).toMatchObject({ checked: true });

    await fireEvent.press(screen.getByRole("button", { name: "Kaydet (1 yok, 1 geç)" }));
    expect(await screen.findByText("Kaydedildi — 0 var, 1 yok, 1 geç. 0 veliye bildirim gitti. 1 öğrencinin velisi kayıtlı değil.")).toBeTruthy();
  });

  it("kaydedilmiş yoklama: kayıtlı durumlar ve kayıt saati", async () => {
    fakeApi({ classrooms: () => json(200, [classA]), today: () => json(200, sheet({ isSaved: true, savedTime: "08:42", students: [student(1, "Absent"), student(2, "Late")] })) });
    await signIn();
    await fireEvent.press(screen.getByRole("button", { name: "Yoklama al" }));

    expect(await screen.findByText("Kaydedildi, saat 08:42")).toBeTruthy();
    expect(screen.getByRole("radio", { name: "Öğrenci 01: Yok" }).props.accessibilityState).toMatchObject({ checked: true });
    expect(screen.getByRole("radio", { name: "Öğrenci 02: Geç" }).props.accessibilityState).toMatchObject({ checked: true });
    expect(screen.getByRole("button", { name: "Kaydet (1 yok, 1 geç)" })).toBeTruthy();
  });
});

it("okul günü değil: liste yok, kaydet yok", async () => {
  fakeApi({ classrooms: () => json(200, [classA]), today: () => json(200, sheet({ isSchoolDay: false, students: [] })) });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Yoklama al" }));

  expect(await screen.findByText("Bugün okul günü değil")).toBeTruthy();
  expect(screen.queryByTestId("attendance-row")).toBeNull();
  expect(screen.queryByRole("button", { name: "Kaydet" })).toBeNull();
});

it("öğrencisi olmayan sınıf: 'Bu sınıfta kayıtlı öğrenci yok', kaydet kapalı", async () => {
  fakeApi({ classrooms: () => json(200, [classA]), today: () => json(200, sheet({ students: [] })) });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Yoklama al" }));

  expect(await screen.findByText("Bu sınıfta kayıtlı öğrenci yok")).toBeTruthy();
  expect(screen.getByRole("button", { name: "Kaydet" }).props.accessibilityState).toMatchObject({ disabled: true });
});

it("erişim yok (atamadan çıkarıldı): 'Bu sayfaya erişiminiz yok.' ve geri Hoş geldin'e", async () => {
  fakeApi({ classrooms: () => json(200, [classA]), today: () => json(404, { detail: "Sınıf bulunamadı" }) });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Yoklama al" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  expect(screen.queryByRole("button", { name: "Tekrar dene" })).toBeNull();
  await fireEvent.press(screen.getByRole("button", { name: "Hoş geldin'e dön" }));
  expect(await screen.findByTestId("welcome")).toBeTruthy();
});

it("öğretmen olmayan kişide 'Yoklama al' yok", async () => {
  const requests = fakeApi({ roles: ["Guardian"] });
  await signIn();

  expect(screen.queryByRole("button", { name: "Yoklama al" })).toBeNull();
  expect(requests.some((r) => r.path.startsWith("/api/attendance"))).toBe(false);
});
