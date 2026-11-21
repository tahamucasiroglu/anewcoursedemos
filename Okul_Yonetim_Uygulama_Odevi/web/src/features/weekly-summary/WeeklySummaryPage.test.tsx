import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import type { Route } from "../admin/adminFake";
import { classA, classB, json, panel, studentList, teacherMe, weeklyFake } from "./weeklySummaryFake";

// Spec 0014 AC-1 ve AC-12 (web, "#/haftalik-ozet"): "Haftalık özet" bağlantısı yalnızca sınıf ataması olan öğretmende; tek sınıfta
// liste doğrudan açılır, birden çokta sınıf seçilir; hafta "Bu hafta" varsayılan, "Geçen hafta" isteğe `week=previous` ekler;
// öğrenci listesi durumları metinle; yükleniyor / boş / hata + tekrar dene; "Size atanmış sınıf yok."; yetkisizde "Bu sayfaya
// erişiminiz yok."; akış yalnızca klavyeyle; iki tema.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

function start(routes: Route[], me: Record<string, unknown> = teacherMe) {
  const api = weeklyFake(routes, me);
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);
  return api;
}

const classrooms: RegExp = /^\/api\/weekly-summaries\/classrooms$/;
const list: RegExp = /^\/api\/weekly-summaries$/;
const student: RegExp = /^\/api\/weekly-summaries\/students\/[^/]+$/;

describe.each(["light", "dark"] as const)("%s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("Hoş geldin → Haftalık özet (tek sınıf): liste Türkçe sırada durumlarla, öğrenci → panel, listeye dön", async () => {
    const api = start([
      ["GET", classrooms, () => json(200, [classA])],
      ["GET", list, () => json(200, studentList())],
      ["GET", student, () => json(200, panel())],
    ]);

    await userEvent.click(await screen.findByRole("link", { name: "Haftalık özet" }));
    expect(await screen.findByRole("heading", { name: "5-A · 16–22 Kasım" })).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "Bu hafta" })).toBeChecked();
    const rows = screen.getAllByRole("listitem");
    expect(rows.map((row) => row.textContent)).toEqual([
      "Ali Yılmaz · Özet yokAç",
      "Ayşe Kaya · Taslak hazırAç",
      "Çınar Ak · GönderildiAç",
    ]);
    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Ali Yılmaz, Özet yok, Aç" }));
    expect(await screen.findByRole("heading", { name: "Ali Yılmaz · 16–22 Kasım" })).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Öğrenci listesine dön" }));
    expect(await screen.findByRole("heading", { name: "5-A · 16–22 Kasım" })).toBeInTheDocument();
    expect(api.requests.filter((r) => r.path === "/api/weekly-summaries")).toHaveLength(2);
  });

  it("birden çok sınıf: sınıf seçilir, 'Geçen hafta' isteğe week=previous ekler", async () => {
    const seen: string[] = [];
    start([
      ["GET", classrooms, () => json(200, [classA, classB])],
      ["GET", list, (request) => {
        const url = new URL(request.url);
        seen.push(`${url.searchParams.get("classroomId")}:${url.searchParams.get("week")}`);
        return json(200, studentList(url.searchParams.get("week") === "previous" ? { weekLabel: "9–15 Kasım", weekStart: "2026-11-09" } : {}));
      }],
    ]);
    window.location.hash = "#/haftalik-ozet";

    const select = await screen.findByRole("combobox", { name: "Sınıf" });
    await userEvent.selectOptions(select, "k2");
    expect(await screen.findByRole("heading", { name: "5-A · 16–22 Kasım" })).toBeInTheDocument();
    await userEvent.click(screen.getByRole("radio", { name: "Geçen hafta" }));
    expect(await screen.findByRole("heading", { name: "5-A · 9–15 Kasım" })).toBeInTheDocument();
    expect(seen).toEqual(["k2:current", "k2:previous"]);
  });

  it("yalnızca klavye: hafta seçimi, öğrenciyi açma ve özet oluşturma", async () => {
    const api = start([
      ["GET", classrooms, () => json(200, [classA])],
      ["GET", list, () => json(200, studentList())],
      ["GET", student, () => json(200, panel())],
      ["POST", /\/draft$/, () => json(200, panel({ status: "Draft", draftId: "d1", draftText: "Taslak metin." }))],
    ]);
    window.location.hash = "#/haftalik-ozet";

    await screen.findByRole("heading", { name: "5-A · 16–22 Kasım" });
    screen.getByRole("radio", { name: "Bu hafta" }).focus();
    await userEvent.tab();
    expect(screen.getByRole("button", { name: "Ali Yılmaz, Özet yok, Aç" })).toHaveFocus();
    await userEvent.keyboard("{Enter}");
    const note = await screen.findByRole("textbox", { name: /Öğretmen notu/ });
    note.focus();
    await userEvent.keyboard("Resimde başarılı.");
    await userEvent.tab();
    expect(screen.getByRole("button", { name: "Özet oluştur" })).toHaveFocus();
    await userEvent.keyboard("{Enter}");
    expect(await screen.findByRole("textbox", { name: /Özet metni/ })).toHaveValue("Taslak metin.");
    expect(api.requests.find((r) => r.method === "POST" && r.path.endsWith("/draft"))?.body).toEqual({ teacherNote: "Resimde başarılı." });
  });

  it("boş sınıf ve liste hatası + tekrar dene", async () => {
    let calls = 0;
    start([
      ["GET", classrooms, () => json(200, [classA])],
      ["GET", list, () => (++calls === 1 ? json(500, { detail: "Beklenmeyen bir hata oluştu." }) : json(200, studentList({ students: [] })))],
    ]);
    window.location.hash = "#/haftalik-ozet";

    expect(await screen.findByRole("alert")).toHaveTextContent("Beklenmeyen bir hata oluştu.");
    await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
    expect(await screen.findByText("Bu sınıfta öğrenci yok")).toBeInTheDocument();
  });

  it("atanmış sınıf yok; sınıf listesi hatası + tekrar dene", async () => {
    let calls = 0;
    start([["GET", classrooms, () => (++calls === 1 ? json(500, { detail: "Sunucu hatası" }) : json(200, []))]]);
    window.location.hash = "#/haftalik-ozet";

    expect(await screen.findByRole("alert")).toHaveTextContent("Sunucu hatası");
    await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
    expect(await screen.findByText("Size atanmış sınıf yok.")).toBeInTheDocument();
  });

  it("bağlantı yalnızca sınıf ataması olan öğretmende; yetkisizde erişim yok", async () => {
    start([], { roles: ["Teacher"], teachesClassroom: false });
    expect(await screen.findByRole("link", { name: "Yoklama al" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Haftalık özet" })).not.toBeInTheDocument();
  });

  it("kurucu ve veli: bağlantı yok, adres açılınca 'Bu sayfaya erişiminiz yok.'", async () => {
    const api = start([], { roles: ["Founder", "Guardian"], teachesClassroom: false });
    expect(await screen.findByRole("link", { name: "İletişim" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Haftalık özet" })).not.toBeInTheDocument();
    window.location.hash = "#/haftalik-ozet";
    expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
    expect(api.requests.some((r) => r.path.startsWith("/api/weekly-summaries"))).toBe(false);
  });
});
