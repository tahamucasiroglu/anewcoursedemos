import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { fakeApi, json, me, tokens } from "./fakeApi";

// Spec 0004 AC-10, AC-8 (web): şubesi olmayan kişi açık mesaj görür; çıkış oturumu kapatır ve girişe döner.
afterEach(() => {
  vi.unstubAllGlobals();
});

it("görebildiği şube yoksa 'Bu okulda size atanmış şube yok.' gösterir; roller Türkçe", async () => {
  vi.stubGlobal("fetch", fakeApi({
    "POST /api/auth/refresh": () => json(200, tokens()),
    "GET /api/me": () => json(200, me({ roles: ["Teacher", "Guardian"], branches: [] })),
  }).fetch);
  render(<App />);

  expect(await screen.findByText("Bu okulda size atanmış şube yok.")).toBeInTheDocument();
  expect(screen.getByText("Öğretmen, Veli")).toBeInTheDocument();
});

it("Çıkış sunucuda oturumu kapatır ve giriş ekranına döner", async () => {
  const api = fakeApi({
    "POST /api/auth/refresh": () => json(200, tokens()),
    "GET /api/me": () => json(200, me()),
    "POST /api/auth/logout": () => new Response(null, { status: 204 }),
  });
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);

  await userEvent.click(await screen.findByRole("button", { name: "Çıkış" }));

  expect(await screen.findByRole("button", { name: "Giriş yap" })).toBeInTheDocument();
  const logout = api.requests.find((r) => r.url.endsWith("/api/auth/logout"))!;
  expect(logout.headers.get("Authorization")).toBe("Bearer erisim-anahtari");
});
