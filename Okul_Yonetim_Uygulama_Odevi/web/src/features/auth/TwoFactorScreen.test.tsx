import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { fakeApi, json, me, tokens } from "./fakeApi";

// Spec 0007 AC-4, AC-13 (web): platform yöneticisi şifreden sonra kod ekranına gelir; boş/eksik kodda istek gitmez;
// "Kod hatalı" ve kilit duyurulur; doğru kodla Hoş geldin + "Platform paneli"; bekleme dolunca yalnızca "Girişe dön".
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

const challenge = { ...tokens(), accessToken: "", refreshToken: null, tenants: [], twoFactorChallenge: "bekleme-1", twoFactorSetupRequired: false };

async function signIn() {
  await userEvent.type(await screen.findByLabelText("E-posta"), "platform@ornek.test");
  await userEvent.type(screen.getByLabelText("Şifre"), "Deneme-Sifre-2026");
  await userEvent.click(screen.getByRole("button", { name: "Giriş yap" }));
}

describe.each(["light", "dark"] as const)("Kod ekranı — %s tema", (mode) => {
  beforeEach(() => {
    window.localStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(document.documentElement.getAttribute("data-theme")).toBe(mode);
  });

  it("kod adımı: alan hatası istek göndermez; 'Kod hatalı' duyurulur; doğru kodla Platform paneli bağlantısı", async () => {
    let attempts = 0;
    const api = fakeApi({
      "POST /api/auth/refresh": () => json(401, {}),
      "POST /api/auth/login": () => json(200, challenge),
      "POST /api/auth/two-factor/verify": () => (++attempts === 1 ? json(400, { detail: "Kod hatalı" }) : json(200, tokens())),
      "GET /api/me": () => json(200, me({ isPlatformAdmin: true })),
    });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);
    await signIn();

    expect(await screen.findByRole("heading", { name: "Doğrulama kodu" })).toBeInTheDocument();
    const code = screen.getByLabelText("6 haneli kod");
    expect(code).toHaveAttribute("autocomplete", "one-time-code");
    expect(code).toHaveAttribute("inputmode", "numeric");
    await userEvent.type(code, "123");
    await userEvent.click(screen.getByRole("button", { name: "Doğrula" }));
    expect(await screen.findByText("6 haneli kodu girin")).toHaveAttribute("role", "alert");
    expect(code).toHaveAttribute("aria-invalid", "true");
    expect(api.paths()).not.toContain("POST /api/auth/two-factor/verify");

    await userEvent.type(code, "456");
    await userEvent.click(screen.getByRole("button", { name: "Doğrula" }));
    expect(await screen.findByText("Kod hatalı")).toHaveAttribute("role", "alert");
    expect(screen.getByLabelText("6 haneli kod")).toHaveAccessibleDescription("Kod hatalı");
    expect(screen.getByLabelText("6 haneli kod")).toHaveAttribute("aria-invalid", "true");
    await userEvent.click(screen.getByRole("button", { name: "Doğrula" }));

    expect(await screen.findByRole("link", { name: "Platform paneli" })).toHaveAttribute("href", "#/platform");
    const sent = api.requests.filter((r) => r.url.endsWith("/api/auth/two-factor/verify"));
    expect(await sent[0].json()).toEqual({ challenge: "bekleme-1", code: "123456", refreshTokenInBody: false });
  });
});

it("kilitli hesap mesajı duyurulur; bekleme süresi dolunca kod alanı kalkar, 'Girişe dön' giriş ekranını açar", async () => {
  let attempts = 0;
  vi.stubGlobal("fetch", fakeApi({
    "POST /api/auth/refresh": () => json(401, {}),
    "POST /api/auth/login": () => json(200, challenge),
    "POST /api/auth/two-factor/verify": () =>
      ++attempts === 1
        ? json(400, { detail: "Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin." })
        : json(401, { detail: "Doğrulama süresi doldu, yeniden giriş yapın." }),
  }).fetch);
  render(<App />);
  await signIn();

  await userEvent.type(await screen.findByLabelText("6 haneli kod"), "123456");
  await userEvent.click(screen.getByRole("button", { name: "Doğrula" }));
  expect(await screen.findByText("Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin.")).toHaveAttribute("role", "alert");
  await userEvent.click(screen.getByRole("button", { name: "Doğrula" }));

  expect(await screen.findByText("Doğrulama süresi doldu, yeniden giriş yapın.")).toBeInTheDocument();
  expect(screen.queryByLabelText("6 haneli kod")).not.toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Girişe dön" }));
  expect(await screen.findByRole("button", { name: "Giriş yap" })).toBeInTheDocument();
});

it("yalnızca klavyeyle: kod yazılıp Enter ile gönderilir", async () => {
  vi.stubGlobal("fetch", fakeApi({
    "POST /api/auth/refresh": () => json(401, {}),
    "POST /api/auth/login": () => json(200, challenge),
    "POST /api/auth/two-factor/verify": () => json(200, tokens()),
    "GET /api/me": () => json(200, me({ isPlatformAdmin: true })),
  }).fetch);
  render(<App />);
  await signIn();

  const code = await screen.findByLabelText("6 haneli kod");
  code.focus();
  await userEvent.keyboard("654321{Enter}");
  expect(await screen.findByRole("link", { name: "Platform paneli" })).toBeInTheDocument();
});

it("platform yöneticisi olmayan kişide 'Platform paneli' bağlantısı yok", async () => {
  vi.stubGlobal("fetch", fakeApi({ "POST /api/auth/refresh": () => json(200, tokens()), "GET /api/me": () => json(200, me()) }).fetch);
  render(<App />);
  expect(await screen.findByRole("heading", { name: /Hoş geldiniz/ })).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: "Platform paneli" })).not.toBeInTheDocument();
});
