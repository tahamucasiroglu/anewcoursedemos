import { fireEvent, render, screen } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";

// Spec 0011 AC-9 ve AC-13 (mobil, öğretmen "Mesai saatlerim"): yalnızca öğretmende; kayıtlı saatler ve günler gelir; alan
// hatalarında (adım dışı saat, başlangıç ≥ bitiş, gün yok) istek gitmez; kaydedince "Kaydedildi" ve gövde hafta sırasıyla;
// "Sessiz modu kapat" kapatır; gün düğmelerinde tam gün adı ve seçili durumu — iki temada.
const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const teacherMe = { fullName: "Elif Öğretmen", email: "elif@ornek.test", tenant: tenantA, roles: ["Teacher"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0, unreadAnnouncementCount: 0, unreadMessageCount: 0 };

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

let puts: unknown[] = [];

let quietGets = 0;

function fakeApi(current: unknown, roles = ["Teacher"], failFirstGet = false) {
  puts = [];
  quietGets = 0;
  globalThis.fetch = (async (request: Request) => {
    const path = new URL(request.url).pathname;
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/login") {
      return json(200, tokens);
    }

    if (path === "/api/me") {
      return json(200, { ...teacherMe, roles });
    }

    if (path === "/api/me/quiet-hours" && request.method === "GET") {
      return ++quietGets === 1 && failFirstGet ? json(500, {}) : json(200, current);
    }

    if (path === "/api/me/quiet-hours" && request.method === "PUT") {
      const body = JSON.parse(await request.clone().text()) as { enabled: boolean };
      puts.push(body);
      return json(200, body.enabled ? body : { enabled: false, start: null, end: null, days: [] });
    }

    return json(404, {});
  }) as typeof fetch;
}

async function openQuietHours() {
  await render(<App />);
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "elif@ornek.test");
  await fireEvent.changeText(screen.getByLabelText("Şifre"), "Sifre-1234");
  await fireEvent.press(screen.getByText("Giriş yap"));
  await fireEvent.press(await screen.findByRole("button", { name: "Mesai saatlerim" }));
  await screen.findByRole("header", { name: "Mesai saatlerim" });
}

beforeEach(async () => {
  reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

describe.each(["light", "dark"] as const)("Mesai saatlerim — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("kayıtlı saatler gelir; değiştirip kaydedince 'Kaydedildi', gövde hafta sırasıyla", async () => {
    fakeApi({ enabled: true, start: "08:30", end: "16:45", days: ["Monday", "Friday"] });
    await openQuietHours();

    expect(await screen.findByDisplayValue("08:30")).toBeTruthy();
    expect(screen.getByDisplayValue("16:45")).toBeTruthy();
    expect(screen.getByText("Sessiz mod açık: mesai dışında gelen veli mesajları bildirim üretmez.")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Pazartesi" }).props.accessibilityState).toMatchObject({ selected: true });
    expect(screen.getByRole("button", { name: "Salı" }).props.accessibilityState).toMatchObject({ selected: false });

    await fireEvent.press(screen.getByRole("button", { name: "Çarşamba" }));
    await fireEvent.changeText(screen.getByLabelText("Bitiş (SS:DD)"), "17:00");
    await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));

    expect(await screen.findByText("Kaydedildi")).toBeTruthy();
    expect(puts).toEqual([{ enabled: true, start: "08:30", end: "17:00", days: ["Monday", "Wednesday", "Friday"] }]);
  });
});

it("alan hataları: adım dışı saat, başlangıç ≥ bitiş, gün yok — istek gitmez", async () => {
  fakeApi({ enabled: false, start: null, end: null, days: [] });
  await openQuietHours();
  expect(await screen.findByText("Sessiz mod kapalı: her veli mesajı bildirim üretir.")).toBeTruthy();

  await fireEvent.changeText(screen.getByLabelText("Başlangıç (SS:DD)"), "08:10");
  await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Başlangıç saati SS:DD biçiminde ve 15 dakikalık adımlarla olmalı")).toBeTruthy();

  await fireEvent.changeText(screen.getByLabelText("Başlangıç (SS:DD)"), "18:00");
  for (const day of ["Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma"]) {
    await fireEvent.press(screen.getByRole("button", { name: day }));
  }

  await fireEvent.press(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Başlangıç saati bitişten önce olmalı")).toBeTruthy();
  expect(screen.getByText("En az bir gün seçilmelidir")).toBeTruthy();
  expect(puts).toEqual([]);
});

it("'Sessiz modu kapat' kapatır", async () => {
  fakeApi({ enabled: true, start: "08:00", end: "17:00", days: ["Monday"] });
  await openQuietHours();
  await fireEvent.press(await screen.findByRole("button", { name: "Sessiz modu kapat" }));

  expect(await screen.findByText("Sessiz mod kapatıldı")).toBeTruthy();
  expect(screen.getByText("Sessiz mod kapalı: her veli mesajı bildirim üretir.")).toBeTruthy();
  expect(puts).toEqual([{ enabled: false, start: null, end: null, days: null }]);
  expect(screen.queryByRole("button", { name: "Sessiz modu kapat" })).toBeNull();
});

it("yükleme hatası: uyarı ve 'Tekrar dene' kayıtlı saatleri getirir", async () => {
  fakeApi({ enabled: true, start: "09:00", end: "15:00", days: ["Monday"] }, ["Teacher"], true);
  await openQuietHours();

  expect(await screen.findByRole("alert")).toHaveTextContent("Mesai saatleri alınamadı.");
  await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByDisplayValue("09:00")).toBeTruthy();
  expect(screen.queryByRole("alert")).toBeNull();
  expect(quietGets).toBe(2);
});
