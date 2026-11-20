import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { json } from "./fakeApi";

// Spec 0006 AC-7, AC-8, AC-13 (web): bağlantı sayfası — denetleniyor, geçersiz + yeni bağlantı iste, alan hataları, kaydediliyor, başarı + girişe git.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
});

function stub(valid: boolean, reset: (request: Request) => Promise<Response> | Response = () => new Response(null, { status: 204 })) {
  const sent: Request[] = [];
  vi.stubGlobal("fetch", async (request: Request) => {
    const path = new URL(request.url, "http://x").pathname;
    sent.push(request.clone());
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/refresh") {
      return json(401, {});
    }

    if (path === "/api/auth/reset-password/check") {
      return json(200, { valid });
    }

    if (path === "/api/auth/reset-password") {
      return reset(request);
    }

    return json(404, {});
  });
  return sent;
}

it("geçersiz ya da süresi dolmuş bağlantı: mesaj ve 'Yeni bağlantı iste'", async () => {
  stub(false);
  window.location.hash = "#/sifre-yenile?anahtar=eski";
  render(<App />);

  expect(await screen.findByRole("alert")).toHaveTextContent("Bu bağlantı geçersiz ya da süresi dolmuş");
  expect(screen.getByRole("link", { name: "Yeni bağlantı iste" })).toHaveAttribute("href", "#/sifremi-unuttum");
});

it("anahtarsız adres istek göndermeden geçersiz sayılır", async () => {
  const sent = stub(true);
  window.location.hash = "#/sifre-yenile";
  render(<App />);

  expect(await screen.findByRole("alert")).toHaveTextContent("Bu bağlantı geçersiz ya da süresi dolmuş");
  expect(sent.some((r) => r.url.includes("reset-password"))).toBe(false);
});

it("geçerli bağlantı: alan hataları istek göndermez; kaydediliyor kilitli; başarı + Girişe git; anahtar yalnızca gövdede gider", async () => {
  let release: (r: Response) => void = () => {};
  const pending = new Promise<Response>((resolve) => { release = resolve; });
  const sent = stub(true, () => pending);
  window.location.hash = "#/sifre-yenile?anahtar=GIZLI%2BANAHTAR";
  render(<App />);
  const submit = await screen.findByRole("button", { name: "Şifreyi kaydet" });

  expect(screen.getByLabelText("Yeni şifre")).toHaveAttribute("autocomplete", "new-password");
  await userEvent.type(screen.getByLabelText("Yeni şifre"), "kisa");
  await userEvent.click(submit);
  expect(screen.getByLabelText("Yeni şifre")).toHaveAccessibleDescription("Şifre en az 8 karakter olmalıdır");
  expect(sent.filter((r) => new URL(r.url, "http://x").pathname === "/api/auth/reset-password")).toHaveLength(0);

  await userEvent.clear(screen.getByLabelText("Yeni şifre"));
  await userEvent.type(screen.getByLabelText("Yeni şifre"), "Yeni-Sifre-2026");
  await userEvent.type(screen.getByLabelText("Yeni şifre (tekrar)"), "Yeni-Sifre-2026");
  await userEvent.click(submit);
  expect(await screen.findByRole("button", { name: "Kaydediliyor…" })).toBeDisabled();
  release(new Response(null, { status: 204 }));

  expect(await screen.findByText("Şifreniz yenilendi, yeni şifrenizle giriş yapabilirsiniz.")).toHaveAttribute("role", "status");
  const request = sent.find((r) => new URL(r.url, "http://x").pathname === "/api/auth/reset-password")!;
  expect(request.url).not.toContain("GIZLI");
  expect(await request.json()).toEqual({ token: "GIZLI+ANAHTAR", newPassword: "Yeni-Sifre-2026", newPasswordRepeat: "Yeni-Sifre-2026" });
  await userEvent.click(screen.getByRole("button", { name: "Girişe git" }));
  expect(window.location.hash).toBe("#/");
});

it("kayıt sırasında bağlantı geçersizleşirse geçersiz bağlantı ekranına döner", async () => {
  stub(true, () => json(400, { detail: "Bu bağlantı geçersiz ya da süresi dolmuş" }));
  window.location.hash = "#/sifre-yenile?anahtar=x";
  render(<App />);
  await userEvent.type(await screen.findByLabelText("Yeni şifre"), "Yeni-Sifre-2026");
  await userEvent.type(screen.getByLabelText("Yeni şifre (tekrar)"), "Yeni-Sifre-2026");
  await userEvent.click(screen.getByRole("button", { name: "Şifreyi kaydet" }));

  expect(await screen.findByRole("link", { name: "Yeni bağlantı iste" })).toBeInTheDocument();
});
