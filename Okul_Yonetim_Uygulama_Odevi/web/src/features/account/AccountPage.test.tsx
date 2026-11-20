import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { adminFake, json, type Route } from "../admin/adminFake";

// Spec 0006 AC-1, AC-3, AC-4, AC-13 (web): Hesabım — profil ve şifre değiştirme; durumlar, alan hataları, iki tema.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

const profile = { fullName: "Ali Öğretmen", email: "ali@ornek.test", phone: null };

function open(routes: Route[], roles: string[] = ["Teacher"]) {
  const api = adminFake([["GET", /^\/api\/account\/profile$/, () => json(200, profile)], ...routes], roles);
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);
  return api;
}

describe.each(["light", "dark"] as const)("Hesabım — %s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));

  it("Hoş geldin'de 'Hesabım' → profil yüklenir; e-posta salt okunur; ad ve telefon kaydedilir, 'Kaydedildi' duyurulur", async () => {
    const api = open([["PUT", /^\/api\/account\/profile$/, () => json(200, { ...profile, fullName: "Ali Veli", phone: "+905321112233" })]]);
    await userEvent.click(await screen.findByRole("link", { name: "Hesabım" }));

    expect(await screen.findByRole("heading", { name: "Hesabım" })).toBeInTheDocument();
    expect(await screen.findByLabelText("E-posta")).toHaveAttribute("readonly");
    await userEvent.clear(screen.getByLabelText("Ad soyad"));
    await userEvent.type(screen.getByLabelText("Ad soyad"), "Ali Veli");
    await userEvent.type(screen.getByLabelText("Telefon"), "0532 111 22 33");
    await userEvent.click(screen.getAllByRole("button", { name: "Kaydet" })[0]);

    expect(await screen.findByText("Kaydedildi")).toHaveAttribute("role", "status");
    expect(await api.sent("PUT", /profile$/)[0].json()).toEqual({ fullName: "Ali Veli", phone: "0532 111 22 33" });
    expect(document.documentElement.getAttribute("data-theme")).toBe(mode);
  });
});

it("profil alan hataları istek göndermez; profil yüklenemezse hata + tekrar dene", async () => {
  let calls = 0;
  const api = adminFake([["GET", /^\/api\/account\/profile$/, () => (++calls === 1 ? json(500, {}) : json(200, profile))]], ["Teacher"]);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/hesabim";
  render(<App />);

  expect(await screen.findByText("Bilgiler alınamadı.")).toHaveAttribute("role", "alert");
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
  await userEvent.clear(await screen.findByLabelText("Ad soyad"));
  await userEvent.type(screen.getByLabelText("Telefon"), "123");
  await userEvent.click(screen.getAllByRole("button", { name: "Kaydet" })[0]);

  expect(screen.getByLabelText("Ad soyad")).toHaveAccessibleDescription("Ad soyad zorunludur");
  expect(screen.getByLabelText("Telefon")).toHaveAccessibleDescription("Geçerli bir telefon numarası girin");
  expect(api.sent("PUT", /profile$/)).toHaveLength(0);
});

it("şifremi değiştir: alan hataları; gönderilirken kilitli; 'Mevcut şifre hatalı' ve başarı duyurulur; şifre alanları autocomplete taşır", async () => {
  let attempt = 0;
  let release: (r: Response) => void = () => {};
  const pending = new Promise<Response>((resolve) => { release = resolve; });
  const api = open([["POST", /change-password$/, () => {
    attempt++;
    if (attempt === 1) {
      return json(400, { detail: "Mevcut şifre hatalı" });
    }

    return pending;
  }]]);
  window.location.hash = "#/hesabim";
  window.dispatchEvent(new HashChangeEvent("hashchange"));
  const submit = await screen.findByRole("button", { name: "Şifreyi değiştir" });

  expect(screen.getByLabelText("Mevcut şifre")).toHaveAttribute("autocomplete", "current-password");
  expect(screen.getByLabelText("Yeni şifre")).toHaveAttribute("autocomplete", "new-password");
  await userEvent.click(submit);
  expect(screen.getByLabelText("Mevcut şifre")).toHaveAccessibleDescription("Mevcut şifre zorunludur");
  await userEvent.type(screen.getByLabelText("Mevcut şifre"), "Eski-Sifre-1");
  await userEvent.type(screen.getByLabelText("Yeni şifre"), "kisa");
  await userEvent.type(screen.getByLabelText("Yeni şifre (tekrar)"), "kisa2");
  await userEvent.click(submit);
  expect(screen.getByLabelText("Yeni şifre")).toHaveAccessibleDescription("Şifre en az 8 karakter olmalıdır");
  expect(screen.getByLabelText("Yeni şifre (tekrar)")).toHaveAccessibleDescription("Şifreler aynı değil");
  expect(api.sent("POST", /change-password$/)).toHaveLength(0);

  await userEvent.clear(screen.getByLabelText("Yeni şifre"));
  await userEvent.type(screen.getByLabelText("Yeni şifre"), "Yeni-Sifre-2026");
  await userEvent.clear(screen.getByLabelText("Yeni şifre (tekrar)"));
  await userEvent.type(screen.getByLabelText("Yeni şifre (tekrar)"), "Yeni-Sifre-2026");
  await userEvent.click(submit);
  expect(await screen.findByText("Mevcut şifre hatalı")).toHaveAttribute("role", "alert");

  await userEvent.click(submit);
  expect(await screen.findByRole("button", { name: "Kaydediliyor…" })).toBeDisabled();
  release(new Response(null, { status: 204 }));
  expect(await screen.findByText("Şifreniz değiştirildi")).toHaveAttribute("role", "status");
  await waitFor(() => expect(screen.getByLabelText("Mevcut şifre")).toHaveValue(""));
});
