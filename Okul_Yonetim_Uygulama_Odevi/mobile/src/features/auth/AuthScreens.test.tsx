import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";

// Spec 0004 (mobil): giriş / okul seç / hoş geldin uygulamanın içinde, iki temada; ağ taklit edilir, güvenli depo bellek içi.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const tenantB = { id: "22222222-2222-4222-8222-222222222222", name: "B Okulu" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });
}

function tokens(extra: Record<string, unknown> = {}) {
  return { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA], ...extra };
}

function me(extra: Record<string, unknown> = {}) {
  return { fullName: "Ayşe Kurucu", email: "ayse@ornek.test", tenant: tenantA, roles: ["Founder"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], ...extra };
}

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
  return { requests, paths: () => requests.map((r) => `${r.method} ${new URL(r.url).pathname}`) };
}

async function login(email: string, password: string) {
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), email);
  await fireEvent.changeText(screen.getByLabelText("Şifre"), password);
  await fireEvent.press(screen.getByText("Giriş yap"));
}

beforeEach(async () => {
  reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

describe.each(["light", "dark"] as const)("Mobil giriş — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("doğru bilgi → hoş geldin; yenileme anahtarı gövdede istenir ve güvenli depoya yazılır", async () => {
    const api = fakeApi({ "POST /api/auth/login": () => json(200, tokens()), "GET /api/me": () => json(200, me()) });
    await render(<App />);

    await login("ayse@ornek.test", "Gizli-Sifre-1");

    expect(await screen.findByText("Hoş geldiniz, Ayşe Kurucu")).toBeTruthy();
    expect(screen.getByText("Kurucu / Yönetici")).toBeTruthy();
    expect(screen.getByText("Merkez")).toBeTruthy();
    expect(await SecureStore.getItemAsync("okul.refreshToken")).toBe("yenileme-1");
    const body = await api.requests.find((r) => r.url.endsWith("/api/auth/login"))!.json();
    expect(body).toEqual({ email: "ayse@ornek.test", password: "Gizli-Sifre-1", refreshTokenInBody: true });
    expect(api.paths()).not.toContain("POST /api/auth/refresh");
  });

  it("alan hatası istek göndermeden duyurulur", async () => {
    const api = fakeApi({});
    await render(<App />);

    await login("gecersiz", "");

    expect(await screen.findByText("Geçerli bir e-posta girin")).toBeTruthy();
    expect(screen.getByText("Şifre zorunludur")).toBeTruthy();
    expect(screen.getAllByRole("alert")).toHaveLength(2);
    expect(api.paths()).not.toContain("POST /api/auth/login");
  });

  it("hatalı bilgi / kilit mesajı sunucudan gelir", async () => {
    fakeApi({ "POST /api/auth/login": () => json(401, { detail: "Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin." }) });
    await render(<App />);

    await login("ayse@ornek.test", "Yanlis-1");

    expect(await screen.findByText("Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin.")).toBeTruthy();
  });

  it("ulaşılamıyorsa mesaj + Tekrar dene", async () => {
    let attempts = 0;
    fakeApi({
      "POST /api/auth/login": () => {
        attempts++;
        return attempts === 1 ? Promise.reject(new TypeError("Network request failed")) : json(200, tokens());
      },
      "GET /api/me": () => json(200, me()),
    });
    await render(<App />);

    await login("ayse@ornek.test", "Gizli-Sifre-1");
    expect(await screen.findByText("Sunucuya ulaşılamıyor. Lütfen daha sonra tekrar deneyin.")).toBeTruthy();
    await fireEvent.press(screen.getByText("Tekrar dene", { exact: true }));

    expect(await screen.findByText("Hoş geldiniz, Ayşe Kurucu")).toBeTruthy();
  });
});

describe("Mobil oturum", () => {
  it("depoda geçerli oturum varsa açılışta doğrudan hoş geldin", async () => {
    await SecureStore.setItemAsync("okul.refreshToken", "kayitli");
    const api = fakeApi({ "POST /api/auth/refresh": () => json(200, tokens({ refreshToken: "yenileme-2" })), "GET /api/me": () => json(200, me()) });
    await render(<App />);

    expect(await screen.findByText("Hoş geldiniz, Ayşe Kurucu")).toBeTruthy();
    expect(await api.requests.find((r) => r.url.endsWith("/api/auth/refresh"))!.json()).toEqual({ refreshToken: "kayitli" });
    expect(await SecureStore.getItemAsync("okul.refreshToken")).toBe("yenileme-2");
  });

  it("iki okul: seç → okul değiştir → öbür okul; şifre tekrar sorulmaz", async () => {
    let selected = tenantA;
    const api = fakeApi({
      "POST /api/auth/login": () => json(200, tokens({ requiresTenantSelection: true, tenants: [tenantA, tenantB] })),
      "POST /api/auth/select-tenant": async (request) => {
        selected = ((await request.json()) as { tenantId: string }).tenantId === tenantB.id ? tenantB : tenantA;
        return json(200, tokens({ tenants: [tenantA, tenantB] }));
      },
      "GET /api/me": () => json(200, me({ tenant: selected, tenants: [tenantA, tenantB] })),
    });
    await render(<App />);

    await login("ayse@ornek.test", "Gizli-Sifre-1");
    await fireEvent.press(await screen.findByText("A Koleji"));
    await fireEvent.press(await screen.findByText("Okul değiştir"));
    await fireEvent.press(await screen.findByText("B Okulu"));

    expect(await screen.findByText("Hoş geldiniz, Ayşe Kurucu")).toBeTruthy();
    expect(screen.getByText("B Okulu")).toBeTruthy();
    expect(screen.queryByLabelText("Şifre")).toBeNull();
    expect(api.paths().filter((p) => p === "POST /api/auth/login")).toHaveLength(1);
  });

  it("şube yoksa açık mesaj; Çıkış güvenli depoyu temizler ve girişe döner", async () => {
    await SecureStore.setItemAsync("okul.refreshToken", "kayitli");
    fakeApi({
      "POST /api/auth/refresh": () => json(200, tokens()),
      "GET /api/me": () => json(200, me({ roles: ["Teacher"], branches: [] })),
      "POST /api/auth/logout": () => new Response(null, { status: 204 }),
    });
    await render(<App />);

    expect(await screen.findByText("Bu okulda size atanmış şube yok.")).toBeTruthy();
    await fireEvent.press(screen.getByText("Çıkış"));

    expect(await screen.findByText("Giriş yap")).toBeTruthy();
    await waitFor(async () => expect(await SecureStore.getItemAsync("okul.refreshToken")).toBeNull());
  });
});
