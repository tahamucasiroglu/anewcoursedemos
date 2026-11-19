import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { fakeApi, json, me, tenantA, tenantB, tokens } from "./fakeApi";

// Spec 0004 AC-4 (web): iki okullu kişi okul seçer, hoş geldin'den okul değiştirir; şifre sorulmaz.
afterEach(() => {
  vi.unstubAllGlobals();
});

it("iki okul: seçim ekranı → seçilen okulun hoş geldini → okul değiştir → öbür okul", async () => {
  let selected = tenantA;
  const api = fakeApi({
    "POST /api/auth/refresh": () => json(401, {}),
    "POST /api/auth/login": () => json(200, tokens({ requiresTenantSelection: true, tenants: [tenantA, tenantB] })),
    "POST /api/auth/select-tenant": async (request) => {
      const { tenantId } = (await request.json()) as { tenantId: string };
      selected = tenantId === tenantB.id ? tenantB : tenantA;
      return json(200, tokens({ tenants: [tenantA, tenantB] }));
    },
    "GET /api/me": () => json(200, me({ tenant: selected, tenants: [tenantA, tenantB] })),
  });
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);
  await userEvent.type(await screen.findByLabelText("E-posta"), "ayse@ornek.test");
  await userEvent.type(screen.getByLabelText("Şifre"), "Gizli-Sifre-1");
  await userEvent.click(screen.getByRole("button", { name: "Giriş yap" }));

  expect(await screen.findByRole("heading", { name: "Okul seçin" })).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "A Koleji" }));
  expect(await screen.findByText("A Koleji")).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Okul değiştir" }));
  await userEvent.click(await screen.findByRole("button", { name: "B Okulu" }));

  expect(await screen.findByText("B Okulu")).toBeInTheDocument();
  expect(screen.queryByText("A Koleji")).not.toBeInTheDocument();
  expect(screen.queryByLabelText("Şifre")).not.toBeInTheDocument();
  expect(api.paths().filter((p) => p === "POST /api/auth/login")).toHaveLength(1);
});

it("seçim başarısızsa hata duyurulur ve liste kalır", async () => {
  const api = fakeApi({
    "POST /api/auth/refresh": () => json(200, tokens({ requiresTenantSelection: true, tenants: [tenantA, tenantB] })),
    "POST /api/auth/select-tenant": () => json(404, { detail: "Okul bulunamadı" }),
  });
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);

  await userEvent.click(await screen.findByRole("button", { name: "B Okulu" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Okul seçilemedi. Lütfen tekrar deneyin.");
  expect(screen.getByRole("button", { name: "A Koleji" })).toBeEnabled();
});
