import { readFileSync } from "node:fs";
import { join } from "node:path";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { adminFake, json, type Route } from "../admin/adminFake";
import { paymentId, receipt } from "./billingFake";

// Spec 0015 AC-8 (web makbuz): gereksinim 7'deki alanlar ve "Bu belge fatura yerine geçmez."; TC kimlik no, IBAN, sağlık bilgisi yok;
// iptal edilmiş makbuz "İPTAL EDİLDİ" (tarih, gerekçe); "Yazdır" tarayıcının yazdırmasını açar; yazdırma görünümünde başlık, sistem
// durumu, menü ve düğmeler basılmaz (@media print); AC-13: yükleniyor / hata, yetkisiz kişi "Bu sayfaya erişiminiz yok.".
afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
  window.location.hash = "";
});

function open(routes: Route[], roles: string[] = ["Founder"]) {
  const api = adminFake(routes, roles);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = `#/makbuz/${paymentId}`;
  render(<App />);
  return api;
}

const receiptPath = /^\/api\/billing\/payments\/[^/]+\/receipt$/;

it("makbuz alanları, dağılım ve 'Bu belge fatura yerine geçmez.'; TC/IBAN/sağlık yok; Yazdır", async () => {
  const print = vi.fn();
  vi.stubGlobal("print", print);
  open([["GET", receiptPath, () => json(200, receipt())]]);

  expect(await screen.findByRole("heading", { name: "Tahsilat makbuzu" })).toBeInTheDocument();
  for (const text of ["A Koleji", "Merkez", "7", "20.11.2026", "Ali Yılmaz", "2026-2027 Eğitim Ücreti", "3.000,00 TL", "Kredi kartı (okul POS'u)", "Ayşe Kurucu"]) {
    expect(screen.getByText(text)).toBeInTheDocument();
  }

  expect(screen.getByText("1. taksit (vade 01.12.2026): 2.833,33 TL")).toBeInTheDocument();
  expect(screen.getByText("2. taksit (vade 01.01.2027): 166,67 TL")).toBeInTheDocument();
  expect(screen.getByText("Bu belge fatura yerine geçmez.")).toBeInTheDocument();
  expect(document.body.textContent).not.toMatch(/TC|IBAN|sağlık|alerji/i);
  expect(screen.queryByText("İPTAL EDİLDİ")).not.toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Yazdır" }));
  expect(print).toHaveBeenCalledTimes(1);
  expect(screen.getByRole("button", { name: "Yazdır" }).closest(".no-print")).not.toBeNull();
});

it("iptal edilmiş makbuz 'İPTAL EDİLDİ' (tarih, gerekçe) gösterir", async () => {
  open([["GET", receiptPath, () => json(200, receipt({ cancelled: true, cancelledAt: "2026-11-20T21:30:00Z", cancelReason: "Yanlış öğrenci" }))]]);
  expect(await screen.findByText("İPTAL EDİLDİ")).toBeInTheDocument();
  expect(screen.getByText("İptal tarihi: 21.11.2026 — Gerekçe: Yanlış öğrenci")).toBeInTheDocument();
});

it("hata durumunda mesaj; yetkisiz kişi 'Bu sayfaya erişiminiz yok.' ve istek gitmez", async () => {
  open([["GET", receiptPath, () => json(404, { detail: "Tahsilat bulunamadı" })]]);
  expect(await screen.findByRole("alert")).toHaveTextContent("Tahsilat bulunamadı");
});

it("öğretmen makbuz adresinde erişim yok", async () => {
  const api = open([], ["Teacher"]);
  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  expect(api.sent("GET", receiptPath)).toHaveLength(0);
});

it("yazdırma görünümü: başlık satırı, sistem durumu ve no-print öğeleri basılmaz", () => {
  const css = readFileSync(join(__dirname, "..", "..", "index.css"), "utf8");
  const print = /@media print\s*\{([\s\S]*?)\n\}/.exec(css)?.[1] ?? "";
  expect(print).toMatch(/\.app-header/);
  expect(print).toMatch(/\.status/);
  expect(print).toMatch(/\.no-print/);
  expect(print).toMatch(/display:\s*none/);
});
