import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { adminFake, branchA, branchB, json, page, type Route } from "../admin/adminFake";
import { classroomA, studentDetail, studentId, studentRow } from "./studentsFake";

// Spec 0009 AC-1 (web: "Öğrenciler" bağlantısı yalnızca kurucuda, başkasına "Bu sayfaya erişiminiz yok.") ve AC-13
// (öğrenci listesi yükleniyor / boş / dolu / hata + tekrar dene, aramada boş, "Ayrıldı" etiketi, sayfalama; ekleme
// formunda alan hataları istek göndermeden ve alanla ilişkili, gönderilirken kilitli düğme, sunucu hatası; yalnızca
// klavyeyle ekleme) — iki temada.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

async function openPage(routes: Route[], roles: string[] = ["Founder"]) {
  const api = adminFake([
    ...routes,
    ["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA, branchB])],
    ["GET", /^\/api\/classrooms$/, () => json(200, [classroomA])],
  ], roles);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/ogrenciler";
  render(<App />);
  return api;
}

describe.each(["light", "dark"] as const)("Öğrenciler — %s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("yükleniyor → hata + tekrar dene → dolu liste; satırda numara, şube, sınıf, veli sayısı, Ayrıldı etiketi", async () => {
    let calls = 0;
    let fail: () => void = () => undefined;
    const items = [studentRow(), studentRow({ id: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb9", fullName: "Ali Er", schoolNumber: null, classroomId: null, classroomName: null, guardianCount: 0, isActive: false })];
    await openPage([["GET", /^\/api\/admin\/students$/, () =>
      ++calls === 1 ? new Promise<Response>((resolve) => { fail = () => resolve(json(500, {})); }) : json(200, page(items))]]);

    expect(await screen.findByText("Yükleniyor…")).toBeInTheDocument();
    fail();
    expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
    await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));

    const rows = await screen.findAllByRole("listitem");
    expect(rows[0]).toHaveTextContent("Zeynep Ak · 301");
    expect(rows[0]).toHaveTextContent("Merkez · 3-A · 2 veli");
    expect(rows[1]).toHaveTextContent("Ali Er Ayrıldı");
    expect(rows[1]).toHaveTextContent("Merkez · Sınıf atanmamış · 0 veli");
    expect(document.body.textContent).not.toContain("Astım");
  });

  it("boş okul: 'Henüz öğrenci yok'; aramada boş: 'Aramaya uyan öğrenci yok' ve arama sunucuya gider", async () => {
    const api = await openPage([["GET", /^\/api\/admin\/students$/, () => json(200, page([]))]]);

    expect(await screen.findByText("Henüz öğrenci yok")).toBeInTheDocument();
    await userEvent.type(screen.getByLabelText("Ad veya okul numarası"), "zz");

    expect(await screen.findByText("Aramaya uyan öğrenci yok")).toBeInTheDocument();
    await waitFor(() => expect(api.sent("GET", /^\/api\/admin\/students$/).some((r) => new URL(r.url).searchParams.get("search") === "zz")).toBe(true));
  });

  it("şube, sınıf ve durum süzgeci ile sayfalama istekleri", async () => {
    const api = await openPage([["GET", /^\/api\/admin\/students$/, (request) =>
      json(200, page([studentRow()], 30, Number(new URL(request.url).searchParams.get("page") ?? 1)))]]);

    await screen.findByText("Sayfa 1 / 2");
    await userEvent.click(screen.getByRole("button", { name: "Sonraki" }));
    await screen.findByText("Sayfa 2 / 2");
    await userEvent.selectOptions(screen.getByLabelText("Şube"), branchA.id);
    await userEvent.selectOptions(screen.getByLabelText("Sınıf"), classroomA.id);
    await userEvent.selectOptions(screen.getByLabelText("Durum"), "left");

    await waitFor(() => {
      const last = new URL(api.sent("GET", /^\/api\/admin\/students$/).at(-1)!.url).searchParams;
      expect([last.get("branchId"), last.get("classroomId"), last.get("status"), last.get("page")]).toEqual([branchA.id, classroomA.id, "left", "1"]);
    });
  });
});

it("kurucu olmayan kişi: Hoş geldin'de 'Öğrenciler' yok, #/ogrenciler'de 'Bu sayfaya erişiminiz yok.' ve istek gitmez", async () => {
  const api = adminFake([], ["Teacher"]);
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);
  expect(await screen.findByRole("heading", { level: 2 })).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: "Öğrenciler" })).not.toBeInTheDocument();

  window.location.hash = "#/ogrenciler";
  window.dispatchEvent(new HashChangeEvent("hashchange"));

  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  expect(api.sent("GET", /^\/api\/admin\/students/)).toHaveLength(0);
});

it("kurucu: Hoş geldin'de 'Öğrenciler' bağlantısı #/ogrenciler'e gider", async () => {
  vi.stubGlobal("fetch", adminFake([]).fetch);
  render(<App />);

  expect(await screen.findByRole("link", { name: "Öğrenciler" })).toHaveAttribute("href", "#/ogrenciler");
});

it("ekleme formu: alan hataları istek göndermeden ve alanla ilişkili; sunucu hatası duyurulur", async () => {
  const api = await openPage([
    ["GET", /^\/api\/admin\/students$/, () => json(200, page([]))],
    ["POST", /^\/api\/admin\/students$/, () => json(400, { detail: "Bu okul numarası başka bir öğrencide" })],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Öğrenci ekle" }));
  await userEvent.type(screen.getByLabelText("Okul numarası"), "12-A");
  await userEvent.type(screen.getByLabelText("Doğum tarihi"), "2999-01-01");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));

  expect(screen.getByLabelText("Ad soyad")).toHaveAccessibleDescription("Ad soyad zorunludur");
  expect(screen.getByLabelText("Ad soyad")).toHaveAttribute("aria-invalid", "true");
  expect(screen.getByLabelText("Doğum tarihi")).toHaveAccessibleDescription("Doğum tarihi bugünden ileri olamaz");
  expect(screen.getByLabelText("Okul numarası")).toHaveAccessibleDescription("Okul numarası yalnızca harf ve rakam içerebilir");
  expect(screen.getByLabelText("Şube")).toHaveAccessibleDescription("Şube seçin");
  expect(api.sent("POST", /^\/api\/admin\/students$/)).toHaveLength(0);

  await userEvent.type(screen.getByLabelText("Ad soyad"), "Yeni Öğrenci");
  await userEvent.clear(screen.getByLabelText("Doğum tarihi"));
  await userEvent.type(screen.getByLabelText("Doğum tarihi"), "2018-05-04");
  await userEvent.clear(screen.getByLabelText("Okul numarası"));
  await userEvent.type(screen.getByLabelText("Okul numarası"), "12A");
  await userEvent.selectOptions(screen.getByLabelText("Şube"), branchA.id);
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Bu okul numarası başka bir öğrencide");
  expect(api.sent("POST", /^\/api\/admin\/students$/)).toHaveLength(1);
});

it.each(["Ş12", "ı5"])("okul numarasında Türkçe harf (%s): yalnız ASCII harf ve rakam — alan hatası, istek gitmez (A-4; 0009 VERIFY V-1)", async (number) => {
  const api = await openPage([["GET", /^\/api\/admin\/students$/, () => json(200, page([]))]]);
  await userEvent.click(await screen.findByRole("button", { name: "Öğrenci ekle" }));
  await userEvent.type(screen.getByLabelText("Ad soyad"), "Yeni Öğrenci");
  await userEvent.type(screen.getByLabelText("Doğum tarihi"), "2018-05-04");
  await userEvent.type(screen.getByLabelText("Okul numarası"), number);
  await userEvent.selectOptions(screen.getByLabelText("Şube"), branchA.id);
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));

  expect(screen.getByLabelText("Okul numarası")).toHaveAccessibleDescription("Okul numarası yalnızca harf ve rakam içerebilir");
  expect(screen.getByLabelText("Okul numarası")).toHaveAttribute("aria-invalid", "true");
  expect(api.sent("POST", /^\/api\/admin\/students$/)).toHaveLength(0);
});

it("ekleme yalnızca klavyeyle: gönderilirken düğme kilitli, kayıttan sonra kart açılır", async () => {
  let release: () => void = () => undefined;
  const api = await openPage([
    ["GET", /^\/api\/admin\/students$/, () => json(200, page([]))],
    ["POST", /^\/api\/admin\/students$/, () => new Promise<Response>((resolve) => { release = () => resolve(json(200, { id: studentId })); })],
    ["GET", /^\/api\/admin\/students\/[^/]+$/, () => json(200, studentDetail({ fullName: "Klavye Öğrenci" }))],
  ]);
  const user = userEvent.setup();
  await screen.findByText("Henüz öğrenci yok");
  screen.getByRole("button", { name: "Öğrenci ekle" }).focus();
  await user.keyboard("{Enter}");
  await user.keyboard("Klavye Öğrenci");
  await user.tab();
  await user.keyboard("2018-05-04");
  await user.tab();
  await user.tab();
  await user.selectOptions(screen.getByLabelText("Şube"), branchA.id);
  screen.getByRole("button", { name: "Kaydet" }).focus();
  await user.keyboard("{Enter}");

  const locked = await screen.findByRole("button", { name: "Kaydediliyor…" });
  expect(locked).toBeDisabled();
  release();
  expect(await screen.findByRole("heading", { name: "Klavye Öğrenci" })).toBeInTheDocument();
  const body = await api.sent("POST", /^\/api\/admin\/students$/)[0].json();
  expect(body).toMatchObject({ fullName: "Klavye Öğrenci", birthDate: "2018-05-04", branchId: branchA.id, classroomId: null, healthNote: null });
});
