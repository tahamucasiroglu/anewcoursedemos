import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { fakeApi, json, me, tokens } from "./fakeApi";

// Spec 0007 AC-3, AC-13 (web): kurulum gereken platform yöneticisi kare kod + elle anahtar görür; kodla kurulum biter.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

const challenge = { ...tokens(), accessToken: "", refreshToken: null, tenants: [], twoFactorChallenge: "bekleme-kur", twoFactorSetupRequired: true };
const secret = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";
const setup = { secret, otpAuthUri: `otpauth://totp/Okul%20Y%C3%B6netim:p%40ornek.test?secret=${secret}&issuer=Okul%20Y%C3%B6netim` };

async function signIn() {
  await userEvent.type(await screen.findByLabelText("E-posta"), "platform@ornek.test");
  await userEvent.type(screen.getByLabelText("Şifre"), "Deneme-Sifre-2026");
  await userEvent.click(screen.getByRole("button", { name: "Giriş yap" }));
}

describe.each(["light", "dark"] as const)("Kurulum ekranı — %s tema", (mode) => {
  beforeEach(() => {
    window.localStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(document.documentElement.getAttribute("data-theme")).toBe(mode);
  });

  it("kare kod ve gruplu anahtar gösterilir; yanlış kod 'Kod hatalı'; doğru kodla kurulum tamamlanır", async () => {
    let confirms = 0;
    const api = fakeApi({
      "POST /api/auth/refresh": () => json(401, {}),
      "POST /api/auth/login": () => json(200, challenge),
      "POST /api/auth/two-factor/setup": () => json(200, setup),
      "POST /api/auth/two-factor/setup/confirm": () => (++confirms === 1 ? json(400, { detail: "Kod hatalı" }) : json(200, tokens())),
      "GET /api/me": () => json(200, me({ isPlatformAdmin: true })),
    });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);
    await signIn();

    expect(await screen.findByRole("heading", { name: "İki adımlı doğrulamayı kur" })).toBeInTheDocument();
    const qr = await screen.findByRole("img", { name: "Doğrulayıcı uygulama için kare kod" });
    expect(qr.getAttribute("src")).toMatch(/^data:image\/svg\+xml/);
    expect(screen.getByText("JBSW Y3DP EHPK 3PXP JBSW Y3DP EHPK 3PXP")).toBeInTheDocument();

    await userEvent.type(screen.getByLabelText("6 haneli kod"), "000000");
    await userEvent.click(screen.getByRole("button", { name: "Doğrula" }));
    expect(await screen.findByText("Kod hatalı")).toHaveAttribute("role", "alert");
    await userEvent.click(screen.getByRole("button", { name: "Doğrula" }));

    expect(await screen.findByRole("link", { name: "Platform paneli" })).toBeInTheDocument();
    expect(api.paths().filter((p) => p === "POST /api/auth/two-factor/setup/confirm")).toHaveLength(2);
    expect(api.paths()).not.toContain("POST /api/auth/two-factor/verify");
  });
});

it("kurulum bilgisi alınamazsa: ulaşılamıyorsa tekrar dene; bekleme dolmuşsa yalnızca girişe dön", async () => {
  let calls = 0;
  vi.stubGlobal("fetch", fakeApi({
    "POST /api/auth/refresh": () => json(401, {}),
    "POST /api/auth/login": () => json(200, challenge),
    "POST /api/auth/two-factor/setup": () =>
      ++calls === 1 ? Promise.reject(new TypeError("ağ")) : json(401, { detail: "Doğrulama süresi doldu, yeniden giriş yapın." }),
  }).fetch);
  render(<App />);
  await signIn();

  expect(await screen.findByText("Sunucuya ulaşılamıyor. Lütfen daha sonra tekrar deneyin.")).toHaveAttribute("role", "alert");
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByText("Doğrulama süresi doldu, yeniden giriş yapın.")).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Tekrar dene" })).not.toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Girişe dön" }));
  expect(await screen.findByRole("button", { name: "Giriş yap" })).toBeInTheDocument();
});
