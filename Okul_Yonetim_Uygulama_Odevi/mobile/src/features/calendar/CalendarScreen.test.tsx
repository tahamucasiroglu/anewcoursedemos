import { fireEvent, render, screen } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";

// Spec 0008 AC-13 (mobil): Hoş geldin'deki "Okul takvimi" düğmesi takvim ekranını açar; yükleniyor / boş (iki mesaj) /
// dolu / hata + tekrar dene iki temada; tür metinle; düğmeler ve başlık erişilebilirlik rolüyle; "Geri" Hoş geldin'e döner.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const me = { fullName: "Veli Kişi", email: "veli@ornek.test", tenant: tenantA, roles: ["Guardian"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA] };
const term = { id: "t1", name: "2026-2027 Güz", startDate: "2026-09-01", endDate: "2027-01-22", isCurrent: true, isUpcoming: false };
const entries = [
  { id: "e1", termId: "t1", title: "Cumhuriyet Bayramı", kind: "Holiday", startDate: "2026-10-29", endDate: "2026-10-29" },
  { id: "e2", termId: "t1", title: "Ara sınav haftası", kind: "Exam", startDate: "2026-11-09", endDate: "2026-11-13" },
];

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });
}

function fakeApi(calendar: Handler) {
  globalThis.fetch = (async (request: Request) => {
    const path = new URL(request.url).pathname;
    switch (`${request.method} ${path}`) {
      case "GET /api/system-health":
        return json(200, { status: "healthy", message: "Sistem çalışıyor" });
      case "POST /api/auth/login":
        return json(200, tokens);
      case "GET /api/me":
        return json(200, me);
      case "GET /api/calendar":
        return calendar(request);
      default:
        return json(404, {});
    }
  }) as typeof fetch;
}

async function openCalendar() {
  await render(<App />);
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "veli@ornek.test");
  await fireEvent.changeText(screen.getByLabelText("Şifre"), "Sifre-1234");
  await fireEvent.press(screen.getByText("Giriş yap"));
  const button = await screen.findByRole("button", { name: "Okul takvimi" });
  await fireEvent.press(button);
}

beforeEach(async () => {
  reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

describe.each(["light", "dark"] as const)("Mobil okul takvimi — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("dolu: dönem başlığı ve tarihleri, kayıtlar tür metniyle ve tarihle sırayla; Geri Hoş geldin'e döner", async () => {
    fakeApi(() => json(200, { term, entries }));
    await openCalendar();

    expect(await screen.findByRole("header", { name: "Okul takvimi" })).toBeTruthy();
    expect(await screen.findByRole("header", { name: "2026-2027 Güz" })).toBeTruthy();
    expect(screen.getByText("01.09.2026 – 22.01.2027")).toBeTruthy();
    const rows = screen.getAllByTestId("calendar-entry");
    expect(rows.map((r) => r.props.accessibilityLabel)).toEqual(["Tatil, Cumhuriyet Bayramı, 29.10.2026", "Sınav, Ara sınav haftası, 09.11.2026 – 13.11.2026"]);
    expect(screen.getByText("Tatil")).toBeTruthy();

    await fireEvent.press(screen.getByRole("button", { name: "Geri" }));
    expect(await screen.findByTestId("welcome")).toBeTruthy();
  });

  it("dönem yoksa 'Henüz dönem tanımlanmadı.'", async () => {
    fakeApi(() => json(200, { term: null, entries: [] }));
    await openCalendar();
    expect(await screen.findByText("Henüz dönem tanımlanmadı.")).toBeTruthy();
  });

  it("yaklaşan dönemde ibare; kayıt yoksa 'Bu dönemde takvim kaydı yok.'", async () => {
    fakeApi(() => json(200, { term: { ...term, isCurrent: false, isUpcoming: true }, entries: [] }));
    await openCalendar();
    expect(await screen.findByText("01.09.2026 – 22.01.2027 · Yaklaşan dönem")).toBeTruthy();
    expect(screen.getByText("Bu dönemde takvim kaydı yok.")).toBeTruthy();
  });

  it("yükleniyor → hata + tekrar dene → dolu", async () => {
    let calls = 0;
    let release: () => void = () => {};
    const first = new Promise<void>((resolve) => {
      release = resolve;
    });
    fakeApi(async () => {
      if (++calls === 1) {
        await first;
        return json(500, {});
      }

      return json(200, { term, entries });
    });
    await openCalendar();

    expect(await screen.findByText("Takvim yükleniyor…")).toBeTruthy();
    release();
    expect(await screen.findByRole("alert")).toBeTruthy();
    expect(screen.getByText("Takvim alınamadı.")).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));
    expect(await screen.findByText("Cumhuriyet Bayramı")).toBeTruthy();
  });
});
