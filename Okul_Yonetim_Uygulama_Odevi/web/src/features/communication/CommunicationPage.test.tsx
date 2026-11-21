import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import type { Route } from "../admin/adminFake";
import { communicationFake, json, results, row } from "./communicationFake";

// Spec 0011 AC-12 (web, "#/iletisim"): "İletişim" bağlantısı yalnızca kurucu ve öğretmende; duyuru listesi yükleniyor / boş /
// dolu / hata + tekrar dene; ayrıntıda "X / Y veli okudu", okumayanlar ve onay sonuçları; veli ve muhasebe "Bu sayfaya
// erişiminiz yok." görür ve istek gitmez; akış yalnızca klavyeyle; iki tema.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

function start(extra: Route[] = [], roles = ["Founder"]) {
  const api = communicationFake(extra, roles);
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);
  return api;
}

describe.each(["light", "dark"] as const)("%s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("kurucu: Hoş geldin → İletişim → liste (X / Y okudu) → klavyeyle ayrıntı: okumayanlar ve onay sonuçları → listeye dön", async () => {
    start();
    await userEvent.click(await screen.findByRole("link", { name: "İletişim" }));
    expect(await screen.findByRole("button", { name: "Veli toplantısı" })).toBeInTheDocument();
    expect(screen.getByText("18 / 22 veli okudu")).toBeInTheDocument();

    screen.getByRole("button", { name: "Veli toplantısı" }).focus();
    await userEvent.keyboard("{Enter}");
    expect(await screen.findByRole("heading", { name: "Müze gezisi" })).toBeInTheDocument();
    expect(screen.getByText("Pazartesi müze gezisi var.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "18 / 22 veli okudu" })).toBeInTheDocument();
    expect(screen.getByText("Veli Kaya (Ayşe Kaya)")).toBeInTheDocument();
    expect(screen.getByText("15 onay, 2 ret, 5 cevapsız")).toBeInTheDocument();
    expect(screen.getByText("Son tarih: 27.11.2026")).toBeInTheDocument();
    expect(screen.getByText("Ali Yılmaz: Onaylıyorum — Veli Yılmaz · 20.11.2026 13:00", { normalizer: (text) => text })).toBeInTheDocument();
    expect(screen.getByText("Ayşe Kaya: Cevapsız")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Duyurulara dön" }));
    expect(await screen.findByRole("button", { name: "Yeni duyuru" })).toBeInTheDocument();
  });

  it("boş liste: 'Henüz duyuru yok'", async () => {
    start([["GET", /^\/api\/announcements$/, () => json(200, [])]], ["Teacher"]);
    window.location.hash = "#/iletisim";
    expect(await screen.findByText("Henüz duyuru yok")).toBeInTheDocument();
  });

  it("hata → Tekrar dene → dolu", async () => {
    let calls = 0;
    start([["GET", /^\/api\/announcements$/, () => (++calls === 1 ? json(500, { detail: "Beklenmeyen hata" }) : json(200, [row({ title: "İkinci deneme" })]))]]);
    window.location.hash = "#/iletisim";
    expect(await screen.findByRole("alert")).toHaveTextContent("Beklenmeyen hata");
    await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
    expect(await screen.findByRole("button", { name: "İkinci deneme" })).toBeInTheDocument();
  });
});

it("yükleniyor durumu liste gelene kadar görünür", async () => {
  let release: () => void = () => {};
  const held = new Promise<void>((resolve) => {
    release = resolve;
  });
  start([["GET", /^\/api\/announcements$/, async () => {
    await held;
    return json(200, [row()]);
  }]]);
  window.location.hash = "#/iletisim";
  expect(await screen.findByText("Yükleniyor…")).toHaveAttribute("aria-busy", "true");
  release();
  expect(await screen.findByRole("button", { name: "Veli toplantısı" })).toBeInTheDocument();
});

it.each([["Guardian"], ["Accounting"]])("%s: bağlantı yok, '#/iletisim' erişim yok ve istek gitmez", async (role) => {
  const api = start([], [role]);
  expect(await screen.findByRole("button", { name: "Çıkış" })).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: "İletişim" })).not.toBeInTheDocument();
  window.location.hash = "#/iletisim";
  expect(await screen.findByText("Bu sayfaya erişiminiz yok.")).toBeInTheDocument();
  expect(api.requests.some((r) => r.path.startsWith("/api/announcements"))).toBe(false);
});

// 0011 VERIFY V-5: ayrıntı alınamazsa hata + "Tekrar dene" ayrıntıyı ve sonuçları yeniden ister.
it("ayrıntı: hata → Tekrar dene → ayrıntı ve sonuçlar", async () => {
  let calls = 0;
  start([["GET", /^\/api\/announcements\/[^/]+\/results$/, () => (++calls === 1 ? json(500, { detail: "Beklenmeyen hata" }) : json(200, results()))]]);
  window.location.hash = "#/iletisim";
  await userEvent.click(await screen.findByRole("button", { name: "Veli toplantısı" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Beklenmeyen hata");
  expect(screen.queryByRole("heading", { name: "Müze gezisi" })).not.toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByRole("heading", { name: "Müze gezisi" })).toBeInTheDocument();
  expect(screen.getByText("15 onay, 2 ret, 5 cevapsız")).toBeInTheDocument();
  expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  expect(calls).toBe(2);
});

it("öğretmende 'İletişim' bağlantısı var", async () => {
  start([], ["Teacher"]);
  expect(await screen.findByRole("link", { name: "İletişim" })).toHaveAttribute("href", "#/iletisim");
});
