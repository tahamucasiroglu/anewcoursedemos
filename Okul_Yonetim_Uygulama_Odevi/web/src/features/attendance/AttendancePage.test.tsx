import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { attendanceFake, classA, classB, json, saveResult, sheet, student } from "./attendanceFake";
import type { Route } from "../admin/adminFake";

// Spec 0010 AC-2, AC-3, AC-4, AC-13 (web, "#/yoklama"): "Yoklama al" yalnızca öğretmende; tek sınıfta liste doğrudan açılır,
// birden çokta Sınıflarım (durumla) → yoklama → "Sınıflarıma dön"; herkes "Var", düğmede sayılar; akış yalnızca klavyeyle
// (Tab + ok tuşları + Enter); kaydediliyor (düğme kilitli) → sonuç duyurulur; hata işaretleri korur; okul günü değil; öğrenci
// yok (kaydet kapalı); kayıtlı saat; erişim yok; boş Sınıflarım; iki tema.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

function start(routes: Route[], roles = ["Teacher"]) {
  const api = attendanceFake(routes, { roles });
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);
  return api;
}

const today: RegExp = /^\/api\/attendance\/classrooms\/[^/]+\/today$/;

describe.each(["light", "dark"] as const)("%s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("Hoş geldin → Yoklama al (tek sınıf): klavyeyle 'Yok' ve 'Geç', Kaydet → kaydediliyor → sonuç; gövde günü ve durumları taşır", async () => {
    let release: () => void = () => {};
    const held = new Promise<void>((resolve) => {
      release = resolve;
    });
    const api = start([
      ["GET", /^\/api\/attendance\/classrooms$/, () => json(200, [classA])],
      ["GET", today, () => json(200, sheet({ students: [student(1), student(2), student(3)] }))],
      ["PUT", today, async () => {
        await held;
        return json(200, saveResult({ present: 1, absent: 1, late: 1, notifiedGuardians: 2, studentsWithoutGuardian: 1 }));
      }],
    ]);

    await userEvent.click(await screen.findByRole("link", { name: "Yoklama al" }));
    expect(await screen.findByRole("heading", { name: "5-A · Merkez" })).toBeInTheDocument();
    expect(screen.getByText("Bugünün yoklaması henüz alınmadı")).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "Öğrenci 01: Var" })).toBeChecked();
    expect(screen.getByRole("button", { name: "Kaydet" })).toBeInTheDocument();

    // Yalnızca klavye: öğrencinin seçili radio'suna odaklan, ok tuşuyla sonraki duruma geç.
    screen.getByRole("radio", { name: "Öğrenci 02: Var" }).focus();
    await userEvent.keyboard("{ArrowRight}");
    expect(screen.getByRole("radio", { name: "Öğrenci 02: Yok" })).toBeChecked();
    await userEvent.tab();
    expect(screen.getByRole("radio", { name: "Öğrenci 03: Var" })).toHaveFocus();
    await userEvent.keyboard("{ArrowLeft}");
    expect(screen.getByRole("radio", { name: "Öğrenci 03: Geç" })).toBeChecked();
    await userEvent.tab();
    expect(screen.getByRole("button", { name: "Kaydet (1 yok, 1 geç)" })).toHaveFocus();
    await userEvent.keyboard("{Enter}");

    const saving = await screen.findByRole("button", { name: "Kaydediliyor…" });
    expect(saving).toBeDisabled();
    expect(screen.getByRole("radio", { name: "Öğrenci 01: Yok" })).toBeDisabled();
    release();
    expect(await screen.findByText("Kaydedildi — 1 var, 1 yok, 1 geç. 2 veliye bildirim gitti. 1 öğrencinin velisi kayıtlı değil.")).toHaveAttribute("role", "status");
    expect(screen.getByText("Kaydedildi, saat 12:00")).toBeInTheDocument();
    const put = api.requests.find((r) => r.method === "PUT")!;
    expect(put.body).toEqual({ day: "2026-11-20", entries: [{ studentId: "s1", status: "Present" }, { studentId: "s2", status: "Absent" }, { studentId: "s3", status: "Late" }] });
  });

  it("birden çok sınıf: Sınıflarım durumla → yoklama (kayıtlı saat ve durumlar) → Sınıflarıma dön listeyi yeniler", async () => {
    const api = start([
      ["GET", /^\/api\/attendance\/classrooms$/, () => json(200, [classB, classA])],
      ["GET", today, () => json(200, sheet({ classroomId: "k2", classroomName: "6-A", branchName: "Avcılar", isSaved: true, savedTime: "08:42", students: [student(1, "Absent")] }))],
    ]);
    window.location.hash = "#/yoklama";

    const rows = await screen.findAllByRole("listitem");
    expect(rows[0]).toHaveTextContent("6-A · Avcılar · Bugün alındı");
    expect(rows[1]).toHaveTextContent("5-A · Merkez · Bugün alınmadı");
    await userEvent.click(within(rows[0]).getByRole("button", { name: "6-A, Avcılar, Bugün alındı, yoklamayı aç" }));

    expect(await screen.findByText("Kaydedildi, saat 08:42")).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "Öğrenci 01: Yok" })).toBeChecked();
    await userEvent.click(screen.getByRole("button", { name: "Sınıflarıma dön" }));
    expect(await screen.findByText("5-A")).toBeInTheDocument();
    expect(api.requests.filter((r) => r.path === "/api/attendance/classrooms")).toHaveLength(2);
  });
});

it("kayıt hatası: işaretler korunur, tekrar Kaydet başarılı", async () => {
  let calls = 0;
  window.location.hash = "#/yoklama";
  start([
    ["GET", /^\/api\/attendance\/classrooms$/, () => json(200, [classA])],
    ["GET", today, () => json(200, sheet())],
    ["PUT", today, () => (++calls === 1 ? json(500, {}) : json(200, saveResult()))],
  ]);

  await userEvent.click(await screen.findByRole("radio", { name: "Öğrenci 01: Yok" }));
  await userEvent.click(screen.getByRole("button", { name: "Kaydet (1 yok)" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Kaydedilemedi; işaretleriniz korunuyor. Tekrar deneyin.");
  expect(screen.getByRole("radio", { name: "Öğrenci 01: Yok" })).toBeChecked();
  await userEvent.click(screen.getByRole("button", { name: "Kaydet (1 yok)" }));
  expect(await screen.findByText("Kaydedildi — 1 var, 1 yok, 0 geç. 2 veliye bildirim gitti.")).toBeInTheDocument();
});

it("okul günü değil: liste ve kaydet yok", async () => {
  window.location.hash = "#/yoklama";
  start([
    ["GET", /^\/api\/attendance\/classrooms$/, () => json(200, [classA])],
    ["GET", today, () => json(200, sheet({ isSchoolDay: false, students: [] }))],
  ]);

  expect(await screen.findByText("Bugün okul günü değil")).toBeInTheDocument();
  expect(screen.queryByRole("radio")).toBeNull();
  expect(screen.queryByRole("button", { name: /Kaydet/ })).toBeNull();
});

it("öğrencisi olmayan sınıf: 'Bu sınıfta kayıtlı öğrenci yok', kaydet kapalı", async () => {
  window.location.hash = "#/yoklama";
  start([
    ["GET", /^\/api\/attendance\/classrooms$/, () => json(200, [classA])],
    ["GET", today, () => json(200, sheet({ students: [] }))],
  ]);

  expect(await screen.findByText("Bu sınıfta kayıtlı öğrenci yok")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Kaydet" })).toBeDisabled();
});

it("yükleniyor → hata + tekrar dene → 'Size atanmış sınıf yok'", async () => {
  let calls = 0;
  let release: () => void = () => {};
  const held = new Promise<void>((resolve) => {
    release = resolve;
  });
  window.location.hash = "#/yoklama";
  start([["GET", /^\/api\/attendance\/classrooms$/, async () => {
    if (++calls === 1) {
      await held;
      return json(500, {});
    }

    return json(200, []);
  }]]);

  expect(await screen.findByText("Yükleniyor…")).toBeInTheDocument();
  release();
  expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByText("Size atanmış sınıf yok")).toBeInTheDocument();
});

it("erişim yok: sınıf 404 → 'Bu sayfaya erişiminiz yok.'; öğretmen olmayan sayfada da aynı mesaj, istek gitmez", async () => {
  window.location.hash = "#/yoklama";
  start([
    ["GET", /^\/api\/attendance\/classrooms$/, () => json(200, [classA])],
    ["GET", today, () => json(404, { detail: "Sınıf bulunamadı" })],
  ]);
  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  vi.unstubAllGlobals();
});

it("öğretmen olmayan kişi: Hoş geldin'de 'Yoklama al' yok; adrese gelirse erişim yok", async () => {
  const api = start([], ["Guardian", "Founder"]);
  expect(await screen.findByRole("link", { name: "Okul takvimi" })).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: "Yoklama al" })).toBeNull();

  window.location.hash = "#/yoklama";
  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  expect(api.requests.some((r) => r.path.startsWith("/api/attendance"))).toBe(false);
});
