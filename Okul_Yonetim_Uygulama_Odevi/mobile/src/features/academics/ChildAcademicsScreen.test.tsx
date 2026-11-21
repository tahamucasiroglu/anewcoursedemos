import { fireEvent, render, screen } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";
import { warmUpFirstRender } from "../../test/warmUp";

// Spec 0013 AC-8 (veli özeti) ve AC-13 (veli ekranları, iki tema): "Ödev ve notlar" yalnızca velide; çocuk seçimi (birden çok
// çocuk, tek çocukta doğrudan, "Kayıtlı çocuğunuz yok"); özetin üç bölümü (bekleyen — en yakın üstte —, son 30 gün durumuyla —
// "Teslim etmedi" yazıyla —, notlar — puan ya da "girilmedi"), bölüm bazında boş yazıları, yükleniyor, hata + tekrar dene,
// "Bu öğrenci artık görüntülenemiyor."; çocuk kartından o çocuğun özetine bağlantı.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const guardianMe = { fullName: "Ayla Ak", email: "ayla@ornek.test", tenant: tenantA, roles: ["Guardian"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0 };
const zeynep = { id: "c1", fullName: "Zeynep Ak", branchName: "Merkez", classroomName: "3-A" };
const can = { id: "c2", fullName: "Can Ak", branchName: "Avcılar", classroomName: null };
const card = { ...zeynep, birthDate: "2018-05-04", schoolNumber: "301", health: { available: false, healthNote: null, allergy: null }, pickupPersons: [] };

function summary(overrides: Record<string, unknown> = {}) {
  return {
    studentId: "c1",
    fullName: "Zeynep Ak",
    today: "2026-11-20",
    pending: [
      { assignmentId: "o2", subject: "Fen", title: "Deney", dueDate: "2026-11-21", status: "Unmarked" },
      { assignmentId: "o1", subject: "Matematik", title: "Kesirler alıştırması", dueDate: "2026-11-25", status: "Unmarked" },
    ],
    recent: [{ assignmentId: "o0", subject: "Türkçe", title: "Okuma", dueDate: "2026-11-18", status: "NotSubmitted" }],
    grades: [
      { assessmentId: "d1", subject: "Türkçe", kind: "Written", title: "1. yazılı", date: "2026-11-20", score: 85 },
      { assessmentId: "d0", subject: "Matematik", kind: "Oral", title: "Sözlü", date: "2026-11-10", score: null },
    ],
    ...overrides,
  };
}

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

function fakeApi(handlers: { list?: Handler; summary?: Handler; card?: Handler; roles?: string[] }) {
  const requests: string[] = [];
  globalThis.fetch = (async (request: Request) => {
    const path = new URL(request.url).pathname;
    requests.push(path);
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/login") {
      return json(200, tokens);
    }

    if (path === "/api/me") {
      return json(200, { ...guardianMe, roles: handlers.roles ?? guardianMe.roles });
    }

    if (path === "/api/my/children" && handlers.list) {
      return handlers.list(request);
    }

    if (/^\/api\/my\/children\/[^/]+\/academics$/.test(path) && handlers.summary) {
      return handlers.summary(request);
    }

    if (/^\/api\/my\/children\/[^/]+$/.test(path) && handlers.card) {
      return handlers.card(request);
    }

    return json(404, {});
  }) as typeof fetch;
  return requests;
}

async function signIn() {
  await render(<App />);
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "ayla@ornek.test");
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

// İlk çizimin soğuk başlangıç bedeli testlerden önce, doğrulamasız bir turda ödenir (B-36, conventions T21; mobile/src/test/warmUp.ts).
warmUpFirstRender(async () => {
  fakeApi({ list: () => json(200, [can, zeynep]), summary: () => json(200, summary()) });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Ödev ve notlar" }));
  await fireEvent.press(await screen.findByRole("button", { name: "Zeynep Ak, Merkez, aç" }));
  await screen.findAllByTestId("summary-assignment");
});

describe.each(["light", "dark"] as const)("Ödev ve notlar (veli) — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("iki çocuk: seçim → yükleniyor → üç bölüm; 'Teslim etmedi' yazıyla; puan ya da girilmedi; çocuklara dönüş", async () => {
    let release: () => void = () => undefined;
    const pending = new Promise<Response>((resolve) => {
      release = () => resolve(json(200, summary()));
    });
    const requests = fakeApi({ list: () => json(200, [can, zeynep]), summary: () => pending });
    await signIn();
    await fireEvent.press(screen.getByRole("button", { name: "Ödev ve notlar" }));
    expect(screen.getByTestId("child-academics-list").type).toBe("RCTScrollView");
    await fireEvent.press(await screen.findByRole("button", { name: "Zeynep Ak, Merkez, aç" }));

    expect(await screen.findByText("Yükleniyor…")).toBeTruthy();
    release();
    expect(await screen.findByRole("header", { name: "Zeynep Ak" })).toBeTruthy();
    expect(screen.getByTestId("child-academics-scroll").type).toBe("RCTScrollView");
    expect(screen.getAllByTestId("summary-assignment").map((row) => row.props.accessibilityLabel)).toEqual([
      "Fen — Deney, teslim 21.11.2026",
      "Matematik — Kesirler alıştırması, teslim 25.11.2026",
      "Türkçe — Okuma, teslim 18.11.2026, Teslim etmedi",
    ]);
    expect(screen.getByText("Teslim etmedi")).toBeTruthy();
    expect(screen.getByText("Türkçe 1. yazılı: 85")).toBeTruthy();
    expect(screen.getByText("Matematik Sözlü: girilmedi")).toBeTruthy();
    expect(screen.getByText("Yazılı · 20.11.2026")).toBeTruthy();
    expect(requests).toContain("/api/my/children/c1/academics");

    await fireEvent.press(screen.getByRole("button", { name: "Çocuklara dön" }));
    expect(await screen.findByRole("button", { name: "Can Ak, Avcılar, aç" })).toBeTruthy();
  });

  it("tek çocuk: doğrudan özet; boş bölümler", async () => {
    fakeApi({ list: () => json(200, [zeynep]), summary: () => json(200, summary({ pending: [], recent: [], grades: [] })) });
    await signIn();
    await fireEvent.press(screen.getByRole("button", { name: "Ödev ve notlar" }));

    expect(await screen.findByText("Bekleyen ödev yok")).toBeTruthy();
    expect(screen.getByText("Son 30 günde ödev yok")).toBeTruthy();
    expect(screen.getByText("Henüz not yok")).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Geri" }));
    expect(await screen.findByTestId("welcome")).toBeTruthy();
  });
});

it("hata + tekrar dene; artık görüntülenemiyor (404)", async () => {
  let calls = 0;
  fakeApi({ list: () => json(200, [zeynep]), summary: () => (++calls === 1 ? json(500, {}) : json(404, { detail: "Bu öğrenci artık görüntülenemiyor." })) });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Ödev ve notlar" }));

  expect(await screen.findByText("Özet yüklenemedi.")).toBeTruthy();
  await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Bu öğrenci artık görüntülenemiyor.");
});

it("kayıtlı çocuk yok", async () => {
  fakeApi({ list: () => json(200, []) });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Ödev ve notlar" }));
  expect(await screen.findByText("Kayıtlı çocuğunuz yok")).toBeTruthy();
});

it("çocuk kartından 'Ödev ve notlar' o çocuğun özetini açar; dönüş karta", async () => {
  fakeApi({ list: () => json(200, [zeynep, can]), card: () => json(200, card), summary: () => json(200, summary()) });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Çocuklarım" }));
  await fireEvent.press((await screen.findAllByTestId("child-row"))[0]);
  await screen.findByTestId("child-card-scroll");
  await fireEvent.press(screen.getByRole("button", { name: "Ödev ve notlar" }));

  expect(await screen.findByRole("header", { name: "Zeynep Ak" })).toBeTruthy();
  expect(screen.getByText("Türkçe 1. yazılı: 85")).toBeTruthy();
  await fireEvent.press(screen.getByRole("button", { name: "Geri" }));
  expect(await screen.findByTestId("child-card-scroll")).toBeTruthy();
});

it("öğretmen rolünde veli menüsü yok", async () => {
  const requests = fakeApi({ roles: ["Teacher"] });
  await signIn();
  expect(screen.queryByRole("button", { name: "Ödev ve notlar" })).toBeNull();
  expect(requests.some((r) => r.includes("/academics"))).toBe(false);
});
