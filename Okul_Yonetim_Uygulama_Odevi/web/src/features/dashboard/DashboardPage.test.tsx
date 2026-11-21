import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { adminFake, json, type Route } from "../admin/adminFake";
import { attendanceCard, branchId, overdueCard, unreadCard } from "./dashboardFake";

// Spec 0017 AC-1 (web: "Yönetici paneli" bağlantısı yalnızca kurucuda; başkası #/panel'de "Bu sayfaya erişiminiz yok." görür ve istek gitmez)
// ve AC-13 (günlük özet: her kart yükleniyor / boş / dolu / hata + tekrar dene — bir kartın hatası ötekileri bozmaz —, "Bugün okul günü değil",
// "Yenile", şube süzgeci, sayılar metinle; akış yalnızca klavyeyle; "Tümünü gör" borç raporuna gider).
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
});

const cards: Route[] = [
  ["GET", /^\/api\/dashboard\/summary\/attendance$/, () => json(200, attendanceCard())],
  ["GET", /^\/api\/dashboard\/summary\/overdue$/, () => json(200, overdueCard())],
  ["GET", /^\/api\/dashboard\/summary\/announcements$/, () => json(200, unreadCard())],
];

function open(routes: Route[], roles: string[] = ["Founder"], hash = "#/panel") {
  const api = adminFake(routes, roles);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = hash;
  render(<App />);
  return api;
}

it.each([
  [["Founder"], true],
  [["Teacher"], false],
  [["Guardian"], false],
  [["Accounting"], false],
])("hoş geldin ekranında 'Yönetici paneli' bağlantısı: %j → %s", async (roles, visible) => {
  open([], roles, "");
  await screen.findByRole("heading", { name: /Hoş geldin/ });
  expect(screen.queryByRole("link", { name: "Yönetici paneli" }) !== null).toBe(visible);
});

it.each([["Teacher"], ["Accounting"], ["Guardian"]])("%s #/panel ve #/panel/raporlar'da 'Bu sayfaya erişiminiz yok.' görür, istek gitmez", async (role) => {
  const api = open(cards, [role]);
  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  window.location.hash = "#/panel/raporlar";
  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  expect(api.sent("GET", /^\/api\/dashboard\//)).toHaveLength(0);
});

it("üç kart dolu: sayılar metinle, alınmayan sınıf ve öğretmeni, yok öğrenciler, geciken tutar 41.500,00, 'X / Y okudu'", async () => {
  open(cards);
  const attendance = await screen.findByRole("region", { name: "Devamsızlık" });
  expect(await within(attendance).findByText("3 sınıfın 2 tanesinde yoklama alındı")).toBeInTheDocument();
  expect(within(attendance).getByText("12 öğrenci yok, 3 öğrenci geç")).toBeInTheDocument();
  expect(within(attendance).getByRole("heading", { name: "Yoklaması alınmayan sınıflar (1)" })).toBeInTheDocument();
  expect(within(attendance).getByText("Merkez / 5-B — Zeynep Öğretmen")).toBeInTheDocument();
  expect(within(attendance).getByText("Ali Yılmaz — Merkez / 5-A")).toBeInTheDocument();

  const overdue = screen.getByRole("region", { name: "Geciken ödemeler" });
  expect(await within(overdue).findByText("18 öğrencinin 41.500,00 TL ödemesi gecikti")).toBeInTheDocument();
  const row = within(overdue).getByRole("row", { name: /Berk Demir/ });
  expect(row).toHaveTextContent("2.333,33");
  expect(row).toHaveTextContent("01.10.2026");
  expect(row).toHaveTextContent("50 gün");
  expect(within(overdue).getByRole("link", { name: "Tümünü gör" })).toHaveAttribute("href", "#/panel/raporlar?rapor=borc&gecikenler=1");

  const unread = screen.getByRole("region", { name: "Okunmayan duyurular" });
  expect(await within(unread).findByText("Son 7 günde 2 duyuruyu 9 veli okumadı")).toBeInTheDocument();
  expect(within(unread).getByRole("link", { name: "Veli toplantısı" })).toHaveAttribute("href", "#/iletisim/duyuru/cccccccc-cccc-4ccc-8ccc-ccccccccccc1");
  expect(unread).toHaveTextContent("5 / 12 okudu");
  expect(screen.getAllByRole("heading", { level: 3 }).map((h) => h.textContent)).toEqual(["Devamsızlık", "Geciken ödemeler", "Okunmayan duyurular"]);
});

it("bir kartın hatası ötekileri bozmaz: hata + Tekrar dene yalnızca o kartta, tekrar deneyince dolar", async () => {
  let failed = true;
  open([
    cards[0],
    ["GET", /^\/api\/dashboard\/summary\/overdue$/, () => (failed ? json(500, {}) : json(200, overdueCard()))],
    cards[2],
  ]);
  const overdue = await screen.findByRole("region", { name: "Geciken ödemeler" });
  expect(await within(overdue).findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
  expect(await screen.findByText("3 sınıfın 2 tanesinde yoklama alındı")).toBeInTheDocument();
  expect(await screen.findByText("Son 7 günde 2 duyuruyu 9 veli okumadı")).toBeInTheDocument();
  expect(screen.getAllByRole("alert")).toHaveLength(1);

  failed = false;
  await userEvent.click(within(overdue).getByRole("button", { name: "Tekrar dene" }));
  expect(await within(overdue).findByText("18 öğrencinin 41.500,00 TL ödemesi gecikti")).toBeInTheDocument();
});

it("boş durumlar: 'Bugün okul günü değil', 'Geciken ödeme yok', 'Okunmayan duyuru yok'; sınıf yoksa 'Henüz sınıf yok'", async () => {
  let schoolDay = false;
  open([
    ["GET", /^\/api\/dashboard\/summary\/attendance$/, () => json(200, attendanceCard(schoolDay ? { classroomCount: 0, takenCount: 0, notTakenCount: 0, notTaken: [] } : { isSchoolDay: false }))],
    ["GET", /^\/api\/dashboard\/summary\/overdue$/, () => json(200, overdueCard({ studentCount: 0, overdueAmount: 0, top: [] }))],
    ["GET", /^\/api\/dashboard\/summary\/announcements$/, () => json(200, unreadCard({ announcementCount: 0, unreadGuardianCount: 0, top: [] }))],
  ]);
  expect(await screen.findByText("Bugün okul günü değil")).toBeInTheDocument();
  expect(await screen.findByText("Geciken ödeme yok")).toBeInTheDocument();
  expect(await screen.findByText("Okunmayan duyuru yok")).toBeInTheDocument();

  schoolDay = true;
  await userEvent.click(screen.getByRole("button", { name: "Yenile" }));
  expect(await screen.findByText("Henüz sınıf yok")).toBeInTheDocument();
});

it("yükleniyor görünür; şube süzgeci ve Yenile üç kartı yeniden ister (şube kimliğiyle)", async () => {
  let release: () => void = () => {};
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  const api = open([
    ["GET", /^\/api\/dashboard\/summary\/attendance$/, async () => {
      await gate;
      return json(200, attendanceCard());
    }],
    cards[1],
    cards[2],
  ]);
  const attendance = await screen.findByRole("region", { name: "Devamsızlık" });
  expect(within(attendance).getByText("Yükleniyor…")).toBeInTheDocument();
  release();
  await within(attendance).findByText("3 sınıfın 2 tanesinde yoklama alındı");

  await userEvent.selectOptions(screen.getByLabelText("Şube"), branchId);
  await screen.findByText("18 öğrencinin 41.500,00 TL ödemesi gecikti");
  for (const card of ["attendance", "overdue", "announcements"]) {
    const last = new URL(api.sent("GET", new RegExp(`^/api/dashboard/summary/${card}$`)).at(-1)!.url);
    expect(last.searchParams.get("branchId")).toBe(branchId);
  }

  const before = api.sent("GET", /^\/api\/dashboard\/summary\//).length;
  await userEvent.click(screen.getByRole("button", { name: "Yenile" }));
  await screen.findByText("18 öğrencinin 41.500,00 TL ödemesi gecikti");
  expect(api.sent("GET", /^\/api\/dashboard\/summary\//).length).toBe(before + 3);
});

it("yalnızca klavyeyle: sekmelerle Raporlar sekmesine geçilir", async () => {
  open([...cards, ["GET", /^\/api\/dashboard\/reports\/attendance$/, () => json(200, { items: [], page: 1, pageSize: 50, totalCount: 0, totals: { absentDays: 0, lateDays: 0 } })], ["GET", /^\/api\/classrooms$/, () => json(200, [])]]);
  await screen.findByText("3 sınıfın 2 tanesinde yoklama alındı");
  const reportsTab = screen.getByRole("tab", { name: "Raporlar" });
  for (let i = 0; i < 40 && document.activeElement !== reportsTab; i++) {
    await userEvent.tab();
  }

  expect(document.activeElement).toBe(reportsTab);

  await userEvent.keyboard("{Enter}");
  expect(await screen.findByText("Bu süzgeçlere uyan kayıt yok")).toBeInTheDocument();
  expect(window.location.hash).toBe("#/panel/raporlar");
  expect(screen.getByRole("tab", { name: "Raporlar" })).toHaveAttribute("aria-selected", "true");
});
