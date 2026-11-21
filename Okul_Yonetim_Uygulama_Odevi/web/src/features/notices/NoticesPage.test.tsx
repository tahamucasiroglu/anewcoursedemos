import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import indexCss from "../../index.css?raw";
import { attendanceFake, json } from "../attendance/attendanceFake";
import type { Route } from "../admin/adminFake";

// Spec 0010 AC-9, AC-13 (web, "#/bildirimler"): "Bildirimler (2)" yalnızca velide; liste sunucunun sırasıyla (en yenisi
// üstte), okunmamış "Yeni" ve kalın, düzeltilen "Düzeltildi"; liste gelince "okundu" isteği ve Hoş geldin sayısı yenilenir;
// boş "Henüz bildirim yok"; hata + tekrar dene; veli olmayan erişemez; iki tema. Spec 0014 review R1-3: haftalık gelişim özeti
// başlığıyla ve satır sonları korunarak (pre-wrap) görünür; diğer bildirimlerde başlık yok.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

const absent = { id: "n1", studentId: "c2", studentName: "Can Ak", day: "2026-11-20", kind: "Absent", text: "Can Ak bugün (20.11.2026) A Koleji'nda yoklamada gelmedi olarak işaretlendi. Bilginiz yoksa okulla iletişime geçin.", isRead: false, at: "2026-11-20T09:05:00Z" };
const summaryText = "Ali bu hafta derslere istekle katıldı.\n\nYapay zekâ desteğiyle hazırlandı, öğretmen onayladı.\nDayanak: 4 gün geldi, 1 gün gelmedi";
const summary = { id: "n3", studentId: "c2", studentName: "Can Ak", day: "2026-11-16", kind: "WeeklySummary", title: "Haftalık gelişim özeti — 16–22 Kasım", text: summaryText, isRead: false, at: "2026-11-20T10:00:00Z" };
const corrected = { id: "n2", studentId: "c1", studentName: "Zeynep Ak", day: "2026-11-19", kind: "Corrected", text: "Zeynep Ak için bugünkü yoklama düzeltildi: okulda.", isRead: true, at: "2026-11-19T09:00:00Z" };

function start(routes: Route[], roles = ["Guardian"], unread: number[] = [0]) {
  const counts = [...unread];
  const api = attendanceFake(routes, () => ({ roles, unreadNoticeCount: counts.length > 1 ? counts.shift() : counts[0] }));
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);
  return api;
}

describe.each(["light", "dark"] as const)("%s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("Hoş geldin → 'Bildirimler (2)': liste etiketlerle; okundu isteği; dönünce 'Bildirimler'", async () => {
    const api = start(
      [
        ["GET", /^\/api\/notices$/, () => json(200, [absent, corrected])],
        ["POST", /^\/api\/notices\/read$/, () => new Response(null, { status: 204 })],
      ],
      ["Guardian"],
      [2, 0],
    );

    await userEvent.click(await screen.findByRole("link", { name: "Bildirimler (2)" }));
    const rows = await screen.findAllByRole("listitem");
    expect(rows[0]).toHaveTextContent(`Yeni · 20.11.2026${absent.text}`);
    expect(rows[0]).toHaveClass("unread");
    expect(rows[1]).toHaveTextContent(`Düzeltildi · 19.11.2026${corrected.text}`);
    expect(rows[1]).not.toHaveClass("unread");
    await vi.waitFor(() => expect(api.requests.some((r) => r.method === "POST" && r.path === "/api/notices/read")).toBe(true));
    // 0010 review F-3: yalnızca gösterilenler ve listenin en yeni zamanı (olduğu gibi) gider.
    expect(api.requests.find((r) => r.path === "/api/notices/read")?.body).toEqual({ upTo: "2026-11-20T09:05:00Z" });

    await userEvent.click(screen.getByRole("link", { name: "Hoş geldin'e dön" }));
    expect(await screen.findByRole("link", { name: "Bildirimler" })).toBeInTheDocument();
  });
});

it("haftalık gelişim özeti başlıklı, satır sonları korunur; diğer bildirimlerde başlık yok (review R1-3)", async () => {
  window.location.hash = "#/bildirimler";
  start([
    ["GET", /^\/api\/notices$/, () => json(200, [summary, absent])],
    ["POST", /^\/api\/notices\/read$/, () => new Response(null, { status: 204 })],
  ]);

  const rows = await screen.findAllByRole("listitem");
  expect(within(rows[0]).getByRole("heading", { level: 3, name: "Haftalık gelişim özeti — 16–22 Kasım" })).toBeInTheDocument();
  const text = within(rows[0]).getByText((_, element) => element?.tagName === "P" && element.textContent === summaryText);
  expect(text).toHaveClass("notice-text-multiline");
  const style = document.createElement("style");
  style.textContent = indexCss;
  document.head.append(style);
  expect(getComputedStyle(text).whiteSpace).toBe("pre-wrap");
  expect(getComputedStyle(within(rows[1]).getByText(absent.text)).whiteSpace).not.toBe("pre-wrap");
  style.remove();
  expect(text.textContent).toContain("\n\nYapay zekâ desteğiyle hazırlandı, öğretmen onayladı.\nDayanak:");
  expect(within(rows[1]).queryByRole("heading")).toBeNull();
  expect(within(rows[1]).getByText(absent.text)).not.toHaveClass("notice-text-multiline");
});

it("bildirim yoksa 'Henüz bildirim yok', okundu isteği gitmez", async () => {
  window.location.hash = "#/bildirimler";
  const api = start([["GET", /^\/api\/notices$/, () => json(200, [])]]);

  expect(await screen.findByText("Henüz bildirim yok")).toBeInTheDocument();
  expect(api.requests.some((r) => r.path === "/api/notices/read")).toBe(false);
});

it("yükleniyor → hata + tekrar dene → liste", async () => {
  let calls = 0;
  let release: () => void = () => {};
  const held = new Promise<void>((resolve) => {
    release = resolve;
  });
  window.location.hash = "#/bildirimler";
  start([["GET", /^\/api\/notices$/, async () => {
    if (++calls === 1) {
      await held;
      return json(500, {});
    }

    return json(200, [corrected]);
  }]]);

  expect(await screen.findByText("Bildirimler yükleniyor…")).toBeInTheDocument();
  release();
  expect(await screen.findByRole("alert")).toHaveTextContent("Bildirimler alınamadı.");
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByText(corrected.text)).toBeInTheDocument();
});

it("veli olmayan: Hoş geldin'de 'Bildirimler' yok; adrese gelirse erişim yok, istek gitmez", async () => {
  const api = start([], ["Teacher"]);
  expect(await screen.findByRole("link", { name: "Yoklama al" })).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: /Bildirimler/ })).toBeNull();

  window.location.hash = "#/bildirimler";
  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  expect(api.requests.some((r) => r.path.startsWith("/api/notices"))).toBe(false);
});
