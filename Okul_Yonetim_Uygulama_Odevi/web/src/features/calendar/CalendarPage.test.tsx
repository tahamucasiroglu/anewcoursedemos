import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { adminFake, examWeek, holiday, json, termAutumn, type Route } from "../admin/adminFake";

// Spec 0008 AC-10, AC-12 (web, "#/takvim"): Hoş geldin'deki "Okul takvimi" bağlantısı; güncel dönem adı ve tarihleri,
// kayıtlar tür metniyle ve tarih/aralıkla; yaklaşan dönem ibaresi; dönem yok / kayıt yok; hata + tekrar dene; geri;
// iki tema.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

function start(routes: Route[], roles = ["Guardian"]) {
  const api = adminFake(routes, roles);
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);
  return api;
}

describe.each(["light", "dark"] as const)("%s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("Hoş geldin → Okul takvimi: dönem başlığı, tarihleri ve kayıtlar sırayla; Geri Hoş geldin'e döner", async () => {
    start([["GET", /^\/api\/calendar$/, () => json(200, { term: termAutumn, entries: [holiday, examWeek] })]]);

    await userEvent.click(await screen.findByRole("link", { name: "Okul takvimi" }));

    expect(await screen.findByRole("heading", { name: "2026-2027 Güz" })).toBeInTheDocument();
    expect(screen.getByText("01.09.2026 – 22.01.2027")).toBeInTheDocument();
    const rows = screen.getAllByRole("listitem");
    expect(rows[0]).toHaveTextContent("Tatil Cumhuriyet Bayramı · 29.10.2026");
    expect(rows[1]).toHaveTextContent("Sınav Ara sınav haftası · 09.11.2026 – 13.11.2026");
    await userEvent.click(screen.getByRole("link", { name: "Hoş geldin'e dön" }));
    expect(await screen.findByRole("heading", { name: /Hoş geldiniz/ })).toBeInTheDocument();
  });

  it("dönem yoksa 'Henüz dönem tanımlanmadı.'", async () => {
    window.location.hash = "#/takvim";
    start([["GET", /^\/api\/calendar$/, () => json(200, { term: null, entries: [] })]]);
    expect(await screen.findByText("Henüz dönem tanımlanmadı.")).toBeInTheDocument();
  });
});

it("yaklaşan dönemde 'Yaklaşan dönem' ibaresi; kayıt yoksa 'Bu dönemde takvim kaydı yok.'", async () => {
  window.location.hash = "#/takvim";
  start([["GET", /^\/api\/calendar$/, () => json(200, { term: { ...termAutumn, isCurrent: false, isUpcoming: true }, entries: [] })]], ["Teacher"]);

  expect(await screen.findByText(/Yaklaşan dönem/)).toBeInTheDocument();
  expect(screen.getByText("Bu dönemde takvim kaydı yok.")).toBeInTheDocument();
});

it("yükleniyor → hata + tekrar dene → takvim", async () => {
  let calls = 0;
  window.location.hash = "#/takvim";
  let release: () => void = () => {};
  const held = new Promise<void>((resolve) => {
    release = resolve;
  });
  start([["GET", /^\/api\/calendar$/, async () => {
    if (++calls === 1) {
      await held;
      return json(500, {});
    }

    return json(200, { term: termAutumn, entries: [holiday] });
  }]], ["Accounting"]);

  expect(await screen.findByText("Takvim yükleniyor…")).toBeInTheDocument();
  release();
  expect(await screen.findByRole("alert")).toHaveTextContent("Takvim alınamadı.");
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByText("Cumhuriyet Bayramı")).toBeInTheDocument();
});
