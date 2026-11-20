import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { adminFake, branchA, branchB, json } from "./adminFake";

// Spec 0005 AC-1 ve AC-13 (web): Yönetim bağlantısı yalnızca kurucuda; kurucu olmayan "erişiminiz yok"; sekmeler; iki tema.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

describe.each(["light", "dark"] as const)("Yönetim sayfası — %s tema", (mode) => {
  beforeEach(() => {
    window.localStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(document.documentElement.getAttribute("data-theme")).toBe(mode);
  });

  it("kurucu Hoş geldin'de 'Yönetim'i görür, tıklayınca Şubeler sekmesi açılır; Kullanıcılar sekmesine geçilir", async () => {
    const api = adminFake([
      ["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA, branchB])],
      ["GET", /^\/api\/admin\/members$/, () => json(200, { items: [], total: 0, page: 1, pageSize: 25 })],
    ]);
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);

    await userEvent.click(await screen.findByRole("link", { name: "Yönetim" }));

    expect(await screen.findByRole("heading", { name: "Yönetim" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Şubeler" })).toHaveAttribute("aria-selected", "true");
    expect(await screen.findByText("Avcılar")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("tab", { name: "Kullanıcılar" }));
    expect(await screen.findByText("Aramaya uyan kişi yok")).toBeInTheDocument();
  });

  it("spec 0008: kurucu Sınıf seviyeleri, Dönemler ve takvim, Sınıflar sekmelerini görür ve geçer", async () => {
    const api = adminFake([
      ["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA])],
      ["GET", /^\/api\/admin\/grade-levels$/, () => json(200, [])],
      ["GET", /^\/api\/admin\/terms$/, () => json(200, [])],
    ]);
    vi.stubGlobal("fetch", api.fetch);
    window.location.hash = "#/yonetim";
    render(<App />);

    await userEvent.click(await screen.findByRole("tab", { name: "Sınıf seviyeleri" }));
    expect(screen.getByRole("tab", { name: "Sınıf seviyeleri" })).toHaveAttribute("aria-selected", "true");
    expect(await screen.findByText("Henüz sınıf seviyesi yok")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("tab", { name: "Dönemler ve takvim" }));
    expect(await screen.findByText("Henüz dönem yok — önce bir dönem ekleyin")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("tab", { name: "Sınıflar" }));
    expect(await screen.findByText("Önce bir dönem ekleyin")).toBeInTheDocument();
    expect(screen.getByRole("tabpanel")).toHaveAttribute("aria-labelledby", "tab-classrooms");
  });
});

it.each([["Teacher"], ["Guardian"], ["Accounting"]])("%s: Yönetim bağlantısı yok; yönetim adresinde 'erişiminiz yok', hiçbir yönetim isteği gitmez", async (role) => {
  const api = adminFake([], [role]);
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);
  expect(await screen.findByRole("heading", { name: /Hoş geldiniz/ })).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: "Yönetim" })).not.toBeInTheDocument();

  window.location.hash = "#/yonetim";
  window.dispatchEvent(new HashChangeEvent("hashchange"));

  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  expect(api.requests.some((r) => r.url.includes("/api/admin/"))).toBe(false);
  expect(within(screen.getByRole("alert").parentElement!).getByRole("link", { name: "Hoş geldin'e dön" })).toBeInTheDocument();
});
