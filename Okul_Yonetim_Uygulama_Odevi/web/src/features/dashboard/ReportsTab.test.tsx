import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { adminFake, json, type Route } from "../admin/adminFake";
import { attendanceReport, balancesReport, branchId, classroomA, classrooms, paymentsReport } from "./dashboardFake";

// Spec 0017 AC-6 (rapor süzgeci: eksik tarih, başlangıç > bitiş, 367 gün alan hatası ve istek gitmez; 366 gün gider; varsayılan bu ayın ilk
// günü – bugün; 50'lik sayfa; "Bu süzgeçlere uyan kayıt yok") ve AC-13 (raporlar: yükleniyor / boş / dolu / hata + tekrar dene, dışa aktarma
// hazırlanıyor — düğme kilitli — / indirildi / "Sonuç çok büyük…" hatası; "Tümünü gör" bağlantısı borç raporunu "yalnızca gecikenler" açar).
beforeEach(() => {
  vi.useFakeTimers({ toFake: ["Date"], now: new Date("2026-11-20T09:00:00Z") });
});

afterEach(() => {
  vi.useRealTimers();
  vi.unstubAllGlobals();
  window.location.hash = "";
});

const base: Route[] = [["GET", /^\/api\/classrooms$/, () => json(200, classrooms)]];

function open(routes: Route[], hash = "#/panel/raporlar") {
  const api = adminFake([...base, ...routes]);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = hash;
  render(<App />);
  return api;
}

function lastQuery(api: ReturnType<typeof open>, path: RegExp) {
  return new URL(api.sent("GET", path).at(-1)!.url).searchParams;
}

function setDate(label: string, value: string) {
  fireEvent.change(screen.getByLabelText(label), { target: { value } });
}

it("varsayılan: devamsızlık raporu bu ayın ilk günü – bugün ile açılır; tablo, okul no ve toplam satırı", async () => {
  const api = open([["GET", /^\/api\/dashboard\/reports\/attendance$/, () => json(200, attendanceReport())]]);
  const table = await screen.findByRole("table", { name: "Devamsızlık raporu" });
  const query = lastQuery(api, /^\/api\/dashboard\/reports\/attendance$/);
  expect(query.get("startDate")).toBe("2026-11-01");
  expect(query.get("endDate")).toBe("2026-11-20");
  expect(query.get("page")).toBe("1");
  expect(within(table).getByRole("row", { name: /Ali Yılmaz/ })).toHaveTextContent("125");
  expect(within(table).getByRole("rowheader", { name: "Toplam (1 öğrenci)" })).toBeInTheDocument();
  expect(screen.getByLabelText("Başlangıç")).toHaveValue("2026-11-01");
});

it.each([
  ["eksik başlangıç", "", "2026-11-20", "Başlangıç tarihi zorunludur"],
  ["başlangıç > bitiş", "2026-11-21", "2026-11-20", "Başlangıç tarihi bitiş tarihinden sonra olamaz"],
  ["367 gün", "2025-11-19", "2026-11-20", "Tarih aralığı en fazla 366 gün olabilir"],
])("süzgeç alan hatası (%s) gösterilir ve istek gitmez", async (_name, start, end, message) => {
  const api = open([["GET", /^\/api\/dashboard\/reports\/attendance$/, () => json(200, attendanceReport())]]);
  await screen.findByRole("table");
  const before = api.sent("GET", /^\/api\/dashboard\/reports\//).length;
  setDate("Başlangıç", start);
  setDate("Bitiş", end);
  await userEvent.click(screen.getByRole("button", { name: "Göster" }));

  expect(await screen.findByText(message)).toBeInTheDocument();
  expect(api.sent("GET", /^\/api\/dashboard\/reports\//)).toHaveLength(before);
  const field = message.startsWith("Başlangıç tarihi zorunludur") ? "Başlangıç" : "Bitiş";
  expect(screen.getByLabelText(field)).toHaveAttribute("aria-invalid", "true");
});

it("366 günlük aralık kabul edilir; şube, sınıf ve eşik sorguya gider; sonuç yoksa 'Bu süzgeçlere uyan kayıt yok'", async () => {
  let calls = 0;
  const api = open([["GET", /^\/api\/dashboard\/reports\/attendance$/, () => json(200, ++calls === 1 ? attendanceReport() : attendanceReport({ items: [], totalCount: 0, totals: { absentDays: 0, lateDays: 0 } }))]]);
  await screen.findByRole("table");
  setDate("Başlangıç", "2025-11-20");
  setDate("Bitiş", "2026-11-20");
  await userEvent.selectOptions(screen.getByLabelText("Şube"), branchId);
  await userEvent.selectOptions(screen.getByLabelText("Sınıf"), classroomA);
  await userEvent.type(screen.getByLabelText("En az yok (gün)"), "3");
  await userEvent.click(screen.getByRole("button", { name: "Göster" }));

  expect(await screen.findByText("Bu süzgeçlere uyan kayıt yok")).toBeInTheDocument();
  const query = lastQuery(api, /^\/api\/dashboard\/reports\/attendance$/);
  expect(query.get("startDate")).toBe("2025-11-20");
  expect(query.get("branchId")).toBe(branchId);
  expect(query.get("classroomId")).toBe(classroomA);
  expect(query.get("minAbsent")).toBe("3");
});

it("tahsilat raporu: iptal 'İptal' yazısıyla, geçerli toplam ve yönteme göre toplamlar; 50'den fazlasında sayfa düğmeleri", async () => {
  const api = open([["GET", /^\/api\/dashboard\/reports\/payments$/, () => json(200, paymentsReport({ totalCount: 120 }))]], "#/panel/raporlar?rapor=tahsilat");
  const table = await screen.findByRole("table", { name: "Tahsilat raporu" });
  expect(within(table).getAllByRole("row", { name: /Ali Yılmaz/ })[1]).toHaveTextContent("İptal");
  expect(within(table).getAllByRole("row", { name: /Ali Yılmaz/ })[0]).toHaveTextContent("1.234,50");
  expect(within(table).getByRole("rowheader", { name: "Geçerli tahsilat toplamı (1 tahsilat)" })).toBeInTheDocument();
  expect(within(table).getByRole("rowheader", { name: "Nakit (1)" })).toBeInTheDocument();
  expect(screen.getByText("Sayfa 1 / 3")).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Sonraki sayfa" }));
  await screen.findByRole("table");
  expect(lastQuery(api, /^\/api\/dashboard\/reports\/payments$/).get("page")).toBe("2");
});

it("'Tümünü gör' bağlantısı borç raporunu 'yalnızca gecikenler' ile açar; tarih alanı yok", async () => {
  const api = open([["GET", /^\/api\/dashboard\/reports\/balances$/, () => json(200, balancesReport())]], "#/panel/raporlar?rapor=borc&gecikenler=1");
  const table = await screen.findByRole("table", { name: "Borç ve gecikme (20.11.2026 itibarıyla)" });
  expect(within(table).getByRole("row", { name: /Berk Demir/ })).toHaveTextContent("01.10.2026");
  expect(lastQuery(api, /^\/api\/dashboard\/reports\/balances$/).get("overdueOnly")).toBe("true");
  expect(screen.getByLabelText("Yalnızca gecikenler")).toBeChecked();
  expect(screen.queryByLabelText("Başlangıç")).not.toBeInTheDocument();
});

it("rapor hatası + Tekrar dene", async () => {
  let failed = true;
  open([["GET", /^\/api\/dashboard\/reports\/attendance$/, () => (failed ? json(500, {}) : json(200, attendanceReport()))]]);
  expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
  failed = false;
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByRole("table")).toBeInTheDocument();
});

it("Excel'e aktar: hazırlanırken düğme kilitli, dosya ekrandaki süzgeçle iner ve adı duyurulur; 'Sonuç çok büyük…' hatası gösterilir", async () => {
  const created: Blob[] = [];
  vi.stubGlobal("URL", Object.assign(URL, { createObjectURL: (blob: Blob) => (created.push(blob), "blob:okul"), revokeObjectURL: () => {} }));
  const downloads: string[] = [];
  const click = vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(function (this: HTMLAnchorElement) {
    downloads.push(this.download);
  });
  let release: () => void = () => {};
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  let tooBig = false;
  const api = open([
    ["GET", /^\/api\/dashboard\/reports\/payments$/, () => json(200, paymentsReport())],
    ["GET", /^\/api\/dashboard\/reports\/payments\/export$/, async () => {
      if (tooBig) {
        return json(400, { detail: "Sonuç çok büyük (en fazla 10.000 satır); tarih aralığını ya da süzgeci daraltın." });
      }

      await gate;
      return new Response("﻿Tarih;Makbuz no\r\n", {
        status: 200,
        headers: { "Content-Type": "text/csv; charset=utf-8", "Content-Disposition": "attachment; filename=tahsilat-raporu-20.11.2026.csv; filename*=UTF-8''tahsilat-raporu-20.11.2026.csv" },
      });
    }],
  ], "#/panel/raporlar?rapor=tahsilat");
  await screen.findByRole("table");

  await userEvent.click(screen.getByRole("button", { name: "Excel'e aktar" }));
  const busy = await screen.findByRole("button", { name: "Hazırlanıyor…" });
  expect(busy).toBeDisabled();
  release();
  expect(await screen.findByText("Dosya indirildi: tahsilat-raporu-20.11.2026.csv")).toBeInTheDocument();
  expect(created).toHaveLength(1);
  expect(downloads).toEqual(["tahsilat-raporu-20.11.2026.csv"]);
  const query = lastQuery(api, /^\/api\/dashboard\/reports\/payments\/export$/);
  expect(query.get("startDate")).toBe("2026-11-01");
  expect(query.get("endDate")).toBe("2026-11-20");

  tooBig = true;
  await userEvent.click(screen.getByRole("button", { name: "Excel'e aktar" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Sonuç çok büyük (en fazla 10.000 satır); tarih aralığını ya da süzgeci daraltın.");
  expect(screen.getByRole("button", { name: "Excel'e aktar" })).toBeEnabled();
  click.mockRestore();
});
