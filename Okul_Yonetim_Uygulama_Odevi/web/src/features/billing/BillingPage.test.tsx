import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { adminFake, json, type Route } from "../admin/adminFake";
import { accountRow, accountsPage, branchId, fee, totals } from "./billingFake";

// Spec 0015 AC-1 (web: "Tahsilat" bağlantısı yalnızca kurucu ve muhasebede; başkası "Bu sayfaya erişiminiz yok.") ve AC-13 (cari listesi
// durumları, süzgeç, sayfa, toplamlar; tarife/indirim sekmeleri yalnızca kurucuda; tarife formu alan hataları istek göndermeden,
// gönderilirken kilitli, sunucu hatası duyurulur; pasifleştirme onaylı; tutar "1.234,50" biçiminde).
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
});

function open(routes: Route[], roles: string[] = ["Founder"], hash = "#/tahsilat") {
  const api = adminFake(routes, roles);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = hash;
  render(<App />);
  return api;
}

it.each([
  [["Founder"], true],
  [["Accounting"], true],
  [["Teacher"], false],
  [["Guardian"], false],
])("hoş geldin ekranında 'Tahsilat' bağlantısı: %j → %s", async (roles, visible) => {
  open([], roles, "");
  await screen.findByRole("heading", { name: /Hoş geldin/ });
  expect(screen.queryByRole("link", { name: "Tahsilat" }) !== null).toBe(visible);
});

it("öğretmen #/tahsilat'ta 'Bu sayfaya erişiminiz yok.' görür ve istek gitmez", async () => {
  const api = open([], ["Teacher"]);
  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  expect(api.sent("GET", /^\/api\/billing\//)).toHaveLength(0);
});

it("cari listesi: yükleniyor → hata + tekrar dene → dolu; tutarlar 1.234,50 biçiminde, alt toplam satırı", async () => {
  let calls = 0;
  open([["GET", /^\/api\/billing\/students$/, () => (++calls === 1 ? json(500, {}) : json(200, accountsPage([accountRow({ totals: totals(12345.5, 1000, 345.5) })], { totals: totals(12345.5, 1000, 345.5) })))]]);

  expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));

  const table = await screen.findByRole("table", { name: "Planı olan öğrenciler" });
  expect(within(table).getByRole("columnheader", { name: "Geciken" })).toBeInTheDocument();
  const row = within(table).getByRole("row", { name: /Ali Yılmaz/ });
  expect(row).toHaveTextContent("12.345,50");
  expect(row).toHaveTextContent("11.345,50");
  expect(within(table).getByRole("link", { name: "Ali Yılmaz" })).toHaveAttribute("href", "#/tahsilat/ogrenci/bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
  expect(within(table).getByRole("rowheader", { name: "Toplam (1 öğrenci)" })).toBeInTheDocument();
});

it("boş listede 'Henüz ödeme planı olan öğrenci yok'", async () => {
  open([["GET", /^\/api\/billing\/students$/, () => json(200, accountsPage([], { totalCount: 0 }))]]);
  expect(await screen.findByText("Henüz ödeme planı olan öğrenci yok")).toBeInTheDocument();
});

it("süzgeç: ad, şube ve yalnızca gecikenler sorguya gider; 25'ten fazlasında sayfa düğmeleri", async () => {
  const api = open([["GET", /^\/api\/billing\/students$/, () => json(200, accountsPage([accountRow()], { totalCount: 30 }))]]);
  await screen.findByRole("table");
  await userEvent.type(screen.getByLabelText("Ad ara"), "yılmaz");
  await userEvent.selectOptions(screen.getByLabelText("Şube"), branchId);
  await userEvent.click(screen.getByLabelText("Yalnızca gecikenler"));
  await userEvent.click(screen.getByRole("button", { name: "Süz" }));
  await screen.findByRole("table");

  const last = new URL(api.sent("GET", /^\/api\/billing\/students$/).at(-1)!.url);
  expect(last.searchParams.get("search")).toBe("yılmaz");
  expect(last.searchParams.get("branchId")).toBe(branchId);
  expect(last.searchParams.get("overdueOnly")).toBe("true");
  expect(screen.getByText("Sayfa 1 / 2")).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Sonraki sayfa" }));
  await screen.findByRole("table");
  expect(new URL(api.sent("GET", /^\/api\/billing\/students$/).at(-1)!.url).searchParams.get("page")).toBe("2");
});

it("muhasebede Tarifeler ve İndirimler sekmesi yok; kurucuda var", async () => {
  open([["GET", /^\/api\/billing\/students$/, () => json(200, accountsPage())]], ["Accounting"]);
  await screen.findByRole("tab", { name: "Öğrenciler" });
  expect(screen.queryByRole("tab", { name: "Tarifeler" })).not.toBeInTheDocument();
  expect(screen.queryByRole("tab", { name: "İndirimler" })).not.toBeInTheDocument();
});

it("tarifeler: boş → ekle; alan hataları istek göndermeden, sunucu hatası duyurulur, başarıda liste yenilenir, tutar metin olarak gider", async () => {
  let rows: unknown[] = [];
  const api = open([
    ["GET", /^\/api\/billing\/students$/, () => json(200, accountsPage())],
    ["GET", /^\/api\/billing\/tariffs$/, () => json(200, rows)],
    ["POST", /^\/api\/billing\/tariffs$/, async (request) => {
      const body = (await request.json()) as { name: string; amount: string };
      if (body.name === "Aynı") {
        return json(400, { detail: "Bu adla etkin bir tarife zaten var." });
      }

      rows = [{ ...fee, name: body.name, amount: Number(body.amount) }];
      return json(200, rows[0]);
    }],
  ]);
  await userEvent.click(await screen.findByRole("tab", { name: "Tarifeler" }));
  expect(await screen.findByText("Henüz tarife yok")).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Tarife ekle" }));

  await userEvent.type(screen.getByLabelText("Yıllık tutar (TL)"), "1,005");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByText("Bu alan zorunludur")).toHaveAttribute("role", "alert");
  expect(screen.getByLabelText("Yıllık tutar (TL)")).toHaveAccessibleDescription("Tutar en fazla 2 ondalık olabilir");
  await userEvent.clear(screen.getByLabelText("Yıllık tutar (TL)"));
  await userEvent.type(screen.getByLabelText("Yıllık tutar (TL)"), "10.000.000,01");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByText("Tutar en fazla 10.000.000,00 TL olabilir")).toBeInTheDocument();
  expect(api.sent("POST", /^\/api\/billing\/tariffs$/)).toHaveLength(0);

  await userEvent.clear(screen.getByLabelText("Yıllık tutar (TL)"));
  await userEvent.type(screen.getByLabelText("Yıllık tutar (TL)"), "85.000,50");
  await userEvent.type(screen.getByLabelText("Tarife adı"), "Aynı");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Bu adla etkin bir tarife zaten var.")).toHaveAttribute("role", "alert");

  await userEvent.clear(screen.getByLabelText("Tarife adı"));
  await userEvent.type(screen.getByLabelText("Tarife adı"), "Eğitim{Enter}");
  expect(await screen.findByText("85.000,50 TL", { exact: false })).toBeInTheDocument();
  const sent = await api.sent("POST", /^\/api\/billing\/tariffs$/).at(-1)!.json();
  expect(sent).toEqual({ name: "Eğitim", amount: "85000.50" });
});

it("tarife kaydedilirken düğme kilitli; pasifleştirme onaylı ve 'Pasif' rozetiyle döner", async () => {
  let active = true;
  const api = open([
    ["GET", /^\/api\/billing\/students$/, () => json(200, accountsPage())],
    ["GET", /^\/api\/billing\/tariffs$/, () => json(200, [{ ...fee, isActive: active }])],
    ["POST", /^\/api\/billing\/tariffs$/, () => new Promise<Response>(() => {})],
    ["POST", /^\/api\/billing\/tariffs\/[^/]+\/deactivate$/, () => {
      active = false;
      return json(200, { ...fee, isActive: false });
    }],
  ]);
  await userEvent.click(await screen.findByRole("tab", { name: "Tarifeler" }));
  await userEvent.click(await screen.findByRole("button", { name: `Pasifleştir: ${fee.name}` }));
  const dialog = screen.getByRole("dialog");
  await userEvent.click(within(dialog).getByRole("button", { name: "Onayla" }));
  expect(await screen.findByText("Pasif")).toBeInTheDocument();
  expect(api.sent("POST", /deactivate$/)).toHaveLength(1);

  await userEvent.click(screen.getByRole("button", { name: "Tarife ekle" }));
  await userEvent.type(screen.getByLabelText("Tarife adı"), "Yemek");
  await userEvent.type(screen.getByLabelText("Yıllık tutar (TL)"), "2400");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByRole("button", { name: "Kaydediliyor…" })).toBeDisabled();
});

it("indirimler: yüzde 0 / 101 / ondalık alan hatası, istek gitmez", async () => {
  const api = open([
    ["GET", /^\/api\/billing\/students$/, () => json(200, accountsPage())],
    ["GET", /^\/api\/billing\/discounts$/, () => json(200, [])],
  ]);
  await userEvent.click(await screen.findByRole("tab", { name: "İndirimler" }));
  expect(await screen.findByText("Henüz indirim yok")).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "İndirim ekle" }));
  await userEvent.type(screen.getByLabelText("İndirim adı"), "Kardeş");
  for (const value of ["0", "101", "12,5"]) {
    await userEvent.clear(screen.getByLabelText("Yüzde (1–100)"));
    await userEvent.type(screen.getByLabelText("Yüzde (1–100)"), value);
    await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
    expect(screen.getByLabelText("Yüzde (1–100)")).toHaveAccessibleDescription("Yüzde 1 ile 100 arasında tam sayı olmalı");
  }

  expect(api.sent("POST", /^\/api\/billing\/discounts$/)).toHaveLength(0);
});
