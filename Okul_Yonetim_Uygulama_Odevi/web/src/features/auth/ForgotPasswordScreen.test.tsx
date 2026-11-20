import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { json } from "./fakeApi";

// Spec 0006 AC-5, AC-13 (web): Giriş'te "Şifremi unuttum" → e-posta → her durumda aynı mesaj; alan hatası; ulaşılamıyor + tekrar dene.
const message = "Bu e-posta sistemde kayıtlıysa şifre yenileme bağlantısı gönderildi. Gelen kutunuzu (ve istenmeyen klasörünü) kontrol edin.";

afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

function stub(forgot: (request: Request) => Promise<Response> | Response) {
  const sent: Request[] = [];
  vi.stubGlobal("fetch", async (request: Request) => {
    const path = new URL(request.url, "http://x").pathname;
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/refresh") {
      return json(401, { detail: "oturum yok" });
    }

    if (path === "/api/auth/forgot-password") {
      sent.push(request.clone());
      return forgot(request);
    }

    return json(404, {});
  });
  return sent;
}

describe.each(["light", "dark"] as const)("Şifremi unuttum — %s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));

  it("girişteki bağlantıdan açılır; geçersiz e-postada istek gitmez; gönderince aynı bilgi mesajı ve Girişe dön", async () => {
    const sent = stub(() => json(200, { message }));
    render(<App />);
    await userEvent.click(await screen.findByRole("link", { name: "Şifremi unuttum" }));

    await userEvent.click(await screen.findByRole("button", { name: "Bağlantı gönder" }));
    expect(screen.getByLabelText("E-posta")).toHaveAccessibleDescription("E-posta zorunludur");
    await userEvent.type(screen.getByLabelText("E-posta"), "gecersiz");
    await userEvent.click(screen.getByRole("button", { name: "Bağlantı gönder" }));
    expect(screen.getByLabelText("E-posta")).toHaveAccessibleDescription("Geçerli bir e-posta girin");
    expect(sent).toHaveLength(0);

    await userEvent.clear(screen.getByLabelText("E-posta"));
    await userEvent.type(screen.getByLabelText("E-posta"), "veli@ornek.test{Enter}");

    expect(await screen.findByText(message)).toHaveAttribute("role", "status");
    expect(await sent[0].json()).toEqual({ email: "veli@ornek.test" });
    expect(screen.getByRole("link", { name: "Girişe dön" })).toHaveAttribute("href", "#/");
    expect(document.documentElement.getAttribute("data-theme")).toBe(mode);
  });
});

it("gönderilirken düğme kilitli; sunucuya ulaşılamazsa hata + tekrar dene", async () => {
  let calls = 0;
  let release: (r: Response) => void = () => {};
  stub(() => {
    calls++;
    if (calls === 1) {
      return Promise.reject(new TypeError("Failed to fetch"));
    }

    return new Promise<Response>((resolve) => { release = resolve; });
  });
  window.location.hash = "#/sifremi-unuttum";
  render(<App />);
  await userEvent.type(await screen.findByLabelText("E-posta"), "veli@ornek.test");
  await userEvent.click(screen.getByRole("button", { name: "Bağlantı gönder" }));

  expect(await screen.findByText("Sunucuya ulaşılamıyor. Lütfen daha sonra tekrar deneyin.")).toHaveAttribute("role", "alert");
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByRole("button", { name: "Gönderiliyor…" })).toBeDisabled();
  release(json(200, { message }));
  expect(await screen.findByText(message)).toHaveAttribute("role", "status");
});
