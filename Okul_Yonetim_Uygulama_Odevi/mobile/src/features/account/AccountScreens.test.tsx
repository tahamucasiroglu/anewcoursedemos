import { fireEvent, render, screen } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";

// Spec 0006 AC-1, AC-3, AC-4, AC-5, AC-13 (mobil): Hesabım (profil, şifre değiştir) ve Şifremi unuttum; iki tema.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const message = "Bu e-posta sistemde kayıtlıysa şifre yenileme bağlantısı gönderildi. Gelen kutunuzu (ve istenmeyen klasörünü) kontrol edin.";

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });
}

const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const me = { fullName: "Ali Öğretmen", email: "ali@ornek.test", tenant: tenantA, roles: ["Teacher"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA] };
const profile = { fullName: "Ali Öğretmen", email: "ali@ornek.test", phone: null };

function fakeApi(routes: Record<string, Handler>) {
  const requests: Request[] = [];
  globalThis.fetch = (async (request: Request) => {
    requests.push(request.clone());
    const path = new URL(request.url).pathname;
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    const route = routes[`${request.method} ${path}`];
    return route ? route(request) : json(404, {});
  }) as typeof fetch;
  return { requests, sent: (key: string) => requests.filter((r) => `${r.method} ${new URL(r.url).pathname}` === key) };
}

async function signIn() {
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "ali@ornek.test");
  await fireEvent.changeText(screen.getByLabelText("Şifre"), "Eski-Sifre-1");
  await fireEvent.press(screen.getByText("Giriş yap"));
}

beforeEach(async () => {
  reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

describe.each(["light", "dark"] as const)("Mobil Hesabım — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("Hoş geldin → Hesabım: profil kaydedilir; şifre değiştirme alan hataları, 'Mevcut şifre hatalı', başarı", async () => {
    let change = 0;
    const api = fakeApi({
      "POST /api/auth/login": () => json(200, tokens),
      "GET /api/me": () => json(200, me),
      "GET /api/account/profile": () => json(200, profile),
      "PUT /api/account/profile": () => json(200, { ...profile, fullName: "Ali Veli" }),
      "POST /api/account/change-password": () => (++change === 1 ? json(400, { detail: "Mevcut şifre hatalı" }) : new Response(null, { status: 204 })),
    });
    await render(<App />);
    await signIn();
    await fireEvent.press(await screen.findByText("Hesabım"));

    expect(await screen.findByText("ali@ornek.test")).toBeTruthy();
    await fireEvent.changeText(screen.getByLabelText("Ad soyad"), "Ali Veli");
    await fireEvent.press(screen.getByText("Kaydet"));
    expect(await screen.findByText("Kaydedildi")).toBeTruthy();
    expect(await api.sent("PUT /api/account/profile")[0].json()).toEqual({ fullName: "Ali Veli", phone: null });

    await fireEvent.press(screen.getByText("Şifreyi değiştir"));
    expect(screen.getByText("Mevcut şifre zorunludur")).toBeTruthy();
    expect(api.sent("POST /api/account/change-password")).toHaveLength(0);
    await fireEvent.changeText(screen.getByLabelText("Mevcut şifre"), "Eski-Sifre-1");
    await fireEvent.changeText(screen.getByLabelText("Yeni şifre"), "Yeni-Sifre-2026");
    await fireEvent.changeText(screen.getByLabelText("Yeni şifre (tekrar)"), "Yeni-Sifre-2026");
    await fireEvent.press(screen.getByText("Şifreyi değiştir"));
    expect(await screen.findByText("Mevcut şifre hatalı")).toBeTruthy();
    await fireEvent.press(screen.getByText("Şifreyi değiştir"));
    expect(await screen.findByText("Şifreniz değiştirildi")).toBeTruthy();
    expect(screen.getByLabelText("Mevcut şifre").props.secureTextEntry).toBe(true);

    await fireEvent.press(screen.getByText("Hoş geldin'e dön"));
    expect(await screen.findByText("Çıkış")).toBeTruthy();
  });
});

it("Hesabım açıkken oturum düşerse yeniden girişte Hoş geldin açılır (review F-6)", async () => {
  fakeApi({
    "POST /api/auth/login": () => json(200, tokens),
    "POST /api/auth/refresh": () => json(401, {}),
    "GET /api/me": () => json(200, me),
    "GET /api/account/profile": () => json(200, profile),
    "PUT /api/account/profile": () => json(401, {}),
  });
  await render(<App />);
  await signIn();
  await fireEvent.press(await screen.findByText("Hesabım"));
  await fireEvent.press(await screen.findByText("Kaydet"));

  expect(await screen.findByText("Giriş yap")).toBeTruthy();
  await signIn();
  expect(await screen.findByText("Çıkış")).toBeTruthy();
  expect(screen.queryByTestId("account")).toBeNull();
});

it("Girişte 'Şifremi unuttum': geçersiz e-postada istek gitmez; gönderince aynı bilgi mesajı; Girişe dön", async () => {
  const api = fakeApi({ "POST /api/auth/forgot-password": () => json(200, { message }) });
  await render(<App />);

  await fireEvent.press(await screen.findByText("Şifremi unuttum"));
  await fireEvent.changeText(screen.getByLabelText("E-posta"), "gecersiz");
  await fireEvent.press(screen.getByText("Bağlantı gönder"));
  expect(screen.getByText("Geçerli bir e-posta girin")).toBeTruthy();
  expect(api.sent("POST /api/auth/forgot-password")).toHaveLength(0);

  await fireEvent.changeText(screen.getByLabelText("E-posta"), "veli@ornek.test");
  await fireEvent.press(screen.getByText("Bağlantı gönder"));
  expect(await screen.findByText(message)).toBeTruthy();
  expect(await api.sent("POST /api/auth/forgot-password")[0].json()).toEqual({ email: "veli@ornek.test" });

  await fireEvent.press(screen.getByText("Girişe dön"));
  expect(await screen.findByText("Giriş yap")).toBeTruthy();
});

it("Şifremi unuttum: sunucuya ulaşılamazsa hata ve tekrar dene", async () => {
  let calls = 0;
  fakeApi({ "POST /api/auth/forgot-password": () => (++calls === 1 ? Promise.reject(new TypeError("Network request failed")) : json(200, { message })) });
  await render(<App />);
  await fireEvent.press(await screen.findByText("Şifremi unuttum"));
  await fireEvent.changeText(screen.getByLabelText("E-posta"), "veli@ornek.test");
  await fireEvent.press(screen.getByText("Bağlantı gönder"));

  expect(await screen.findByText("Sunucuya ulaşılamıyor. Lütfen daha sonra tekrar deneyin.")).toBeTruthy();
  await fireEvent.press(screen.getByText("Tekrar dene"));
  expect(await screen.findByText(message)).toBeTruthy();
});

it("Platform yöneticisi mobilde: kod adımı yerine 'web'den giriş yapın' mesajı; Girişe dön", async () => {
  const api = fakeApi({
    "POST /api/auth/login": () => json(200, { ...tokens, accessToken: "", refreshToken: null, tenants: [], twoFactorChallenge: "bekleme", twoFactorSetupRequired: false }),
  });
  await render(<App />);
  await signIn();

  expect(await screen.findByText(/Platform yöneticisi hesabıyla web'den giriş yapın/)).toBeTruthy();
  expect(await SecureStore.getItemAsync("okul.refreshToken")).toBeNull();
  expect(api.sent("POST /api/auth/two-factor/verify")).toHaveLength(0);
  await fireEvent.press(screen.getByText("Girişe dön"));
  expect(await screen.findByText("Giriş yap")).toBeTruthy();
});
