import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { adminFake, json, type Route } from "../admin/adminFake";
import { account, aliId, discount, fee, paymentId, paymentRow, plan, planId } from "./billingFake";

// Spec 0015 AC-13 (web, öğrenci carisi): yükleniyor / hata + tekrar dene / dolu (özet, taksit tablosu "Gecikti" metniyle, tahsilatlar,
// hareket dökümü); plan formu taksit önizlemesiyle, alan hataları istek göndermeden, sunucu hatası ("Net tutar 0 TL; …") duyurulur;
// tahsilat formu (kalan borç sınırı, tarih, yöntem; gönderilirken kilitli; başarıda "Makbuza git"); tahsilat iptali gerekçeyle, Esc
// kapatır; planı iptal et onaylı; planı olmayan öğrencide "Henüz ödeme planı yok".
afterEach(() => {
  vi.unstubAllGlobals();
  vi.useRealTimers();
  window.location.hash = "";
});

const accountPath = /^\/api\/billing\/students\/[^/]+$/;

function open(routes: Route[], roles: string[] = ["Accounting"]) {
  const api = adminFake(routes, roles);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = `#/tahsilat/ogrenci/${aliId}`;
  render(<App />);
  return api;
}

it("hata + tekrar dene → özet, taksitler ('Gecikti' metinle), tahsilatlar ve hareket dökümü", async () => {
  let calls = 0;
  open([["GET", accountPath, () => (++calls === 1 ? json(500, {}) : json(200, account()))]]);
  expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));

  expect(await screen.findByRole("heading", { name: "Ali Yılmaz — cari" })).toBeInTheDocument();
  expect(screen.getByText("8.500,00 TL")).toBeInTheDocument();
  expect(screen.getByText("1.333,33 TL")).toBeInTheDocument();
  const installments = screen.getAllByRole("table")[0];
  expect(within(installments).getByRole("row", { name: /^1 01\.11\.2026/ })).toHaveTextContent("Gecikti");
  expect(within(installments).getByRole("row", { name: /^2 01\.12\.2026/ })).toHaveTextContent("Bekliyor");
  expect(screen.getByRole("link", { name: "7 numaralı makbuz" })).toHaveAttribute("href", `#/makbuz/${paymentId}`);
  expect(screen.getByText("Tahsilat — 2026-2027 Eğitim Ücreti (makbuz 7)")).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Öğrenci listesi" })).toHaveAttribute("href", "#/tahsilat");
});

it("planı olmayan öğrenci: 'Henüz ödeme planı yok', tahsilat ekle düğmesi yok", async () => {
  open([["GET", accountPath, () => json(200, account({ plans: [], payments: [], movements: [] }))]]);
  expect(await screen.findByText("Henüz ödeme planı yok")).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Tahsilat ekle" })).not.toBeInTheDocument();
});

it("plan aç: alan hataları istek göndermeden; önizleme tabloyu gösterir; sunucu hatası duyurulur; başarıda 'Plan açıldı.'", async () => {
  vi.useFakeTimers({ toFake: ["Date"], now: new Date("2026-11-20T09:00:00Z") });
  let created = false;
  const api = open([
    ["GET", accountPath, () => json(200, account({ plans: created ? [plan()] : [], payments: [], movements: [] }))],
    ["GET", /^\/api\/billing\/tariffs$/, () => json(200, [fee, { ...fee, id: "x", name: "Pasif tarife", isActive: false }])],
    ["GET", /^\/api\/billing\/discounts$/, () => json(200, [discount, { ...discount, id: "y", name: "Tam burs", percent: 100 }])],
    ["POST", /^\/api\/billing\/plans\/preview$/, () => json(200, { feeName: fee.name, feeAmount: 10000, discountName: "Kardeş", discountPercent: 15, discountAmount: 1500, net: 8500, installments: [
      { sequence: 1, dueDate: "2026-12-01", amount: 2833.33 },
      { sequence: 2, dueDate: "2027-01-01", amount: 2833.33 },
      { sequence: 3, dueDate: "2027-02-01", amount: 2833.34 },
    ] })],
    ["POST", /^\/api\/billing\/plans$/, async (request) => {
      const body = (await request.json()) as { discountId: string | null };
      if (body.discountId === "y") {
        return json(400, { detail: "Net tutar 0 TL; tam burslu öğrenci için plan açılmaz." });
      }

      created = true;
      return json(200, { id: planId });
    }],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Plan aç" }));
  const form = await screen.findByRole("form", { name: "Taksit planı aç" });
  expect(within(form).queryByRole("option", { name: /Pasif tarife/ })).not.toBeInTheDocument();
  await userEvent.clear(within(form).getByLabelText("Taksit sayısı (1–12)"));
  await userEvent.type(within(form).getByLabelText("Taksit sayısı (1–12)"), "13");
  await userEvent.click(within(form).getByRole("button", { name: "Önizle" }));
  expect(within(form).getByLabelText("Tarife")).toHaveAccessibleDescription("Tarife seçin");
  expect(within(form).getByLabelText("Taksit sayısı (1–12)")).toHaveAccessibleDescription("Taksit sayısı 1 ile 12 arasında olmalı");
  await userEvent.clear(within(form).getByLabelText("İlk vade"));
  await userEvent.type(within(form).getByLabelText("İlk vade"), "2026-11-19");
  await userEvent.click(within(form).getByRole("button", { name: "Planı aç" }));
  expect(within(form).getByLabelText("İlk vade")).toHaveAccessibleDescription("İlk vade bugün ile 365 gün sonrası arasında olmalı");
  expect(api.sent("POST", /^\/api\/billing\/plans/)).toHaveLength(0);

  await userEvent.selectOptions(within(form).getByLabelText("Tarife"), fee.id);
  await userEvent.selectOptions(within(form).getByLabelText("İndirim (isteğe bağlı)"), discount.id);
  await userEvent.clear(within(form).getByLabelText("Taksit sayısı (1–12)"));
  await userEvent.type(within(form).getByLabelText("Taksit sayısı (1–12)"), "3");
  await userEvent.clear(within(form).getByLabelText("İlk vade"));
  await userEvent.type(within(form).getByLabelText("İlk vade"), "2026-12-01");
  await userEvent.click(within(form).getByRole("button", { name: "Önizle" }));
  expect(await within(form).findByText("Tarife 10.000,00 TL · indirim 1.500,00 TL · net 8.500,00 TL")).toBeInTheDocument();
  expect(within(form).getByRole("row", { name: /^3 01\.02\.2027 2\.833,34$/ })).toBeInTheDocument();

  await userEvent.selectOptions(within(form).getByLabelText("İndirim (isteğe bağlı)"), "y");
  expect(within(form).queryByText(/net 8\.500,00/)).not.toBeInTheDocument();
  await userEvent.click(within(form).getByRole("button", { name: "Planı aç" }));
  expect(await within(form).findByText("Net tutar 0 TL; tam burslu öğrenci için plan açılmaz.")).toHaveAttribute("role", "alert");

  await userEvent.selectOptions(within(form).getByLabelText("İndirim (isteğe bağlı)"), discount.id);
  await userEvent.click(within(form).getByRole("button", { name: "Planı aç" }));
  expect(await screen.findByText("Plan açıldı.")).toBeInTheDocument();
  expect(await api.sent("POST", /^\/api\/billing\/plans$/).at(-1)!.json()).toEqual({ studentId: aliId, feeScheduleId: fee.id, discountId: discount.id, installmentCount: 3, firstDueDate: "2026-12-01" });
});

it("tahsilat ekle: kalan borçtan fazla, gelecek tarih, yöntem yok → istek gitmez; gönderilirken kilitli; başarıda makbuz bağlantısı", async () => {
  vi.useFakeTimers({ toFake: ["Date"], now: new Date("2026-11-20T09:00:00Z") });
  let release: (r: Response) => void = () => {};
  const pending = new Promise<Response>((resolve) => {
    release = resolve;
  });
  const api = open([
    ["GET", accountPath, () => json(200, account())],
    ["POST", /^\/api\/billing\/payments$/, () => pending],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Tahsilat ekle" }));
  const form = screen.getByRole("form", { name: "Tahsilat ekle" });
  expect(within(form).getByText(/Kalan borç: 7\.000,00 TL/)).toBeInTheDocument();
  await userEvent.type(within(form).getByLabelText("Tutar (TL)"), "7.000,01");
  await userEvent.clear(within(form).getByLabelText("Tarih"));
  await userEvent.type(within(form).getByLabelText("Tarih"), "2026-11-21");
  await userEvent.click(within(form).getByRole("button", { name: "Kaydet" }));
  expect(within(form).getByLabelText("Tutar (TL)")).toHaveAccessibleDescription("Tutar kalan borçtan fazla olamaz.");
  expect(within(form).getByLabelText("Tarih")).toHaveAccessibleDescription("Tarih gelecekte olamaz");
  expect(within(form).getByLabelText("Yöntem")).toHaveAccessibleDescription("Ödeme yöntemi seçin");
  expect(api.sent("POST", /^\/api\/billing\/payments$/)).toHaveLength(0);

  await userEvent.clear(within(form).getByLabelText("Tutar (TL)"));
  await userEvent.type(within(form).getByLabelText("Tutar (TL)"), "7.000");
  await userEvent.clear(within(form).getByLabelText("Tarih"));
  await userEvent.type(within(form).getByLabelText("Tarih"), "2026-11-20");
  await userEvent.selectOptions(within(form).getByLabelText("Yöntem"), "BankTransfer");
  await userEvent.click(within(form).getByRole("button", { name: "Kaydet" }));
  expect(await within(form).findByRole("button", { name: "Kaydediliyor…" })).toBeDisabled();
  expect(api.sent("POST", /^\/api\/billing\/payments$/)).toHaveLength(1);
  expect(await api.sent("POST", /^\/api\/billing\/payments$/)[0].json()).toEqual({ planId, amount: "7000.00", date: "2026-11-20", method: "BankTransfer", note: null });

  release(json(200, { id: paymentId, receiptNo: 8 }));
  expect(await screen.findByText(/Tahsilat kaydedildi\. Makbuz no 8\./)).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Makbuza git" })).toHaveAttribute("href", `#/makbuz/${paymentId}`);
});

it("tahsilat formunda sunucu hatası duyurulur (araya başka tahsilat girdiyse)", async () => {
  vi.useFakeTimers({ toFake: ["Date"], now: new Date("2026-11-20T09:00:00Z") });
  open([
    ["GET", accountPath, () => json(200, account())],
    ["POST", /^\/api\/billing\/payments$/, () => json(400, { detail: "Tutar kalan borçtan fazla olamaz." })],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Tahsilat ekle" }));
  const form = screen.getByRole("form", { name: "Tahsilat ekle" });
  await userEvent.type(within(form).getByLabelText("Tutar (TL)"), "100");
  await userEvent.selectOptions(within(form).getByLabelText("Yöntem"), "Cash");
  await userEvent.click(within(form).getByRole("button", { name: "Kaydet" }));
  expect(await within(form).findByText("Tutar kalan borçtan fazla olamaz.")).toHaveAttribute("role", "alert");
});

it("tahsilat iptali: gerekçesiz istek gitmez; Esc kapatır; gerekçeyle gönderilir ve cari yenilenir ('İPTAL EDİLDİ')", async () => {
  let cancelled = false;
  const api = open([
    ["GET", accountPath, () => json(200, account({ payments: [paymentRow(cancelled ? { cancelled: true, cancelledAt: "2026-11-20T09:00:00Z", cancelReason: "Yanlış giriş" } : {})] }))],
    ["POST", /^\/api\/billing\/payments\/[^/]+\/cancel$/, () => {
      cancelled = true;
      return json(200, { id: paymentId });
    }],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "7 numaralı tahsilatı iptal et" }));
  let dialog = screen.getByRole("dialog");
  expect(within(dialog).getByLabelText("İptal gerekçesi")).toHaveFocus();
  await userEvent.keyboard("{Escape}");
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

  await userEvent.click(screen.getByRole("button", { name: "7 numaralı tahsilatı iptal et" }));
  dialog = screen.getByRole("dialog");
  await userEvent.click(within(dialog).getByRole("button", { name: "Tahsilatı iptal et" }));
  expect(within(dialog).getByLabelText("İptal gerekçesi")).toHaveAccessibleDescription("İptal gerekçesi zorunludur");
  expect(api.sent("POST", /cancel$/)).toHaveLength(0);

  await userEvent.type(within(dialog).getByLabelText("İptal gerekçesi"), "Yanlış giriş");
  await userEvent.click(within(dialog).getByRole("button", { name: "Tahsilatı iptal et" }));
  expect(await screen.findByText(/İPTAL EDİLDİ · 20\.11\.2026 · Yanlış giriş/)).toBeInTheDocument();
  expect(await api.sent("POST", /cancel$/)[0].json()).toEqual({ reason: "Yanlış giriş" });
});

it("planı iptal et onay ister; sunucu reddederse mesaj duyurulur", async () => {
  open([
    ["GET", accountPath, () => json(200, account())],
    ["POST", /^\/api\/billing\/plans\/[^/]+\/cancel$/, () => json(400, { detail: "Bu planda tahsilat var; önce tahsilatları iptal edin." })],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Planı iptal et" }));
  await userEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Onayla" }));
  expect(await screen.findByText("Bu planda tahsilat var; önce tahsilatları iptal edin.")).toHaveAttribute("role", "alert");
});
