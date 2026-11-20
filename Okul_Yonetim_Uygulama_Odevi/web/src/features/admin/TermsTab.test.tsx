import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { adminFake, branchA, examWeek, holiday, json, termAutumn, termSpring, type Route } from "./adminFake";

// Spec 0008 AC-12 (web, Dönemler ve takvim sekmesi): dönem listesi durumları iki temada (Güncel/Yaklaşan etiketi), dönem
// formu alan hataları istek göndermeden ve sunucu hatası; Takvim alt paneli: kayıt listesi (tür metinle), boş durum,
// kayıt formu (dönem dışı gün istek göndermeden), silme onayı.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

async function openTab(routes: Route[]) {
  const api = adminFake([...routes, ["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA])]]);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/yonetim";
  render(<App />);
  await userEvent.click(await screen.findByRole("tab", { name: "Dönemler ve takvim" }));
  return api;
}

describe.each(["light", "dark"] as const)("%s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("yükleniyor → hata + tekrar dene → liste; Güncel ve Yaklaşan etiketleri, tarihler", async () => {
    let calls = 0;
    await openTab([["GET", /^\/api\/admin\/terms$/, () => (++calls === 1 ? json(500, {}) : json(200, [{ ...termSpring, isUpcoming: true }, termAutumn]))]]);

    expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
    await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));

    const rows = await screen.findAllByRole("listitem");
    expect(rows[0]).toHaveTextContent("2026-2027 Bahar · 08.02.2027 – 18.06.2027 Yaklaşan");
    expect(rows[1]).toHaveTextContent("2026-2027 Güz · 01.09.2026 – 22.01.2027 Güncel");
  });

  it("boş listede 'Henüz dönem yok — önce bir dönem ekleyin'", async () => {
    await openTab([["GET", /^\/api\/admin\/terms$/, () => json(200, [])]]);
    expect(await screen.findByText("Henüz dönem yok — önce bir dönem ekleyin")).toBeInTheDocument();
  });
});

it("dönem ekle: bitiş başlangıçtan önce ve 367 gün istek göndermeden; çakışma sunucu hatası; başarıda liste yenilenir", async () => {
  let terms = [termAutumn];
  const api = await openTab([
    ["GET", /^\/api\/admin\/terms$/, () => json(200, terms)],
    ["POST", /^\/api\/admin\/terms$/, async (request) => {
      const body = (await request.json()) as { name: string; startDate: string; endDate: string };
      if (body.startDate <= termAutumn.endDate) {
        return json(400, { detail: "Bu tarihler başka bir dönemle çakışıyor" });
      }

      terms = [{ ...termSpring, name: body.name }, ...terms];
      return json(200, terms[0]);
    }],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Dönem ekle" }));
  expect(screen.getByLabelText("Başlangıç")).toHaveAttribute("type", "date");

  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByText("Dönem adı zorunludur")).toHaveAttribute("role", "alert");
  expect(screen.getByLabelText("Başlangıç")).toHaveAccessibleDescription("Başlangıç ve bitiş günü zorunludur");

  await userEvent.type(screen.getByLabelText("Dönem adı"), "Bahar");
  fireEvent.change(screen.getByLabelText("Başlangıç"), { target: { value: "2027-02-08" } });
  fireEvent.change(screen.getByLabelText("Bitiş"), { target: { value: "2027-02-01" } });
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByLabelText("Bitiş")).toHaveAccessibleDescription("Bitiş günü başlangıç gününden sonra olmalı");

  fireEvent.change(screen.getByLabelText("Bitiş"), { target: { value: "2028-02-09" } });
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByLabelText("Bitiş")).toHaveAccessibleDescription("Dönem en fazla 366 gün sürebilir");
  expect(api.sent("POST", /terms$/)).toHaveLength(0);

  fireEvent.change(screen.getByLabelText("Başlangıç"), { target: { value: "2027-01-22" } });
  fireEvent.change(screen.getByLabelText("Bitiş"), { target: { value: "2027-06-18" } });
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Bu tarihler başka bir dönemle çakışıyor")).toHaveAttribute("role", "alert");

  fireEvent.change(screen.getByLabelText("Başlangıç"), { target: { value: "2027-02-08" } });
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Bahar")).toBeInTheDocument();
  expect(screen.queryByLabelText("Dönem adı")).not.toBeInTheDocument();
});

it("Takvim alt paneli: kayıtlar tür metniyle ve tarihle; dönem dışı gün istek göndermeden; silme onaylı", async () => {
  let entries = [holiday, examWeek];
  const api = await openTab([
    ["GET", /^\/api\/admin\/terms$/, () => json(200, [termAutumn])],
    ["GET", /^\/api\/admin\/terms\/[^/]+\/calendar-entries$/, () => json(200, entries)],
    ["DELETE", /^\/api\/admin\/calendar-entries\/[^/]+$/, () => {
      entries = [holiday];
      return new Response(null, { status: 204 });
    }],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Takvim" }));

  const panel = await screen.findByRole("region", { name: "Takvim: 2026-2027 Güz" });
  const rows = await within(panel).findAllByRole("listitem");
  expect(rows[0]).toHaveTextContent("29.10.2026 · Tatil Cumhuriyet Bayramı");
  expect(rows[1]).toHaveTextContent("09.11.2026 – 13.11.2026 · Sınav Ara sınav haftası");

  await userEvent.click(within(panel).getByRole("button", { name: "Kayıt ekle" }));
  await userEvent.type(within(panel).getByLabelText("Başlık"), "Yaz okulu");
  await userEvent.selectOptions(within(panel).getByLabelText("Tür"), "Etkinlik");
  fireEvent.change(within(panel).getByLabelText("Başlangıç"), { target: { value: "2027-01-20" } });
  fireEvent.change(within(panel).getByLabelText("Bitiş"), { target: { value: "2027-01-25" } });
  await userEvent.click(within(panel).getByRole("button", { name: "Kaydet" }));
  expect(within(panel).getByLabelText("Bitiş")).toHaveAccessibleDescription("Kayıt dönemin dışında");
  expect(api.sent("POST", /calendar-entries$/)).toHaveLength(0);
  await userEvent.click(within(panel).getByRole("button", { name: "Vazgeç" }));

  await userEvent.click(within(panel).getAllByRole("button", { name: "Sil" })[1]);
  expect(screen.getByRole("dialog")).toHaveTextContent("Takvim kaydı silinsin mi?");
  await userEvent.click(screen.getByRole("button", { name: "Onayla" }));
  await waitFor(() => expect(api.sent("DELETE", /calendar-entries/)).toHaveLength(1));
  await waitFor(() => expect(within(panel).queryByText("Ara sınav haftası")).not.toBeInTheDocument());
});

it("Takvim alt paneli: kayıt yoksa 'Bu dönemde takvim kaydı yok.'; Takvimi kapat paneli kapatır", async () => {
  await openTab([
    ["GET", /^\/api\/admin\/terms$/, () => json(200, [termAutumn])],
    ["GET", /^\/api\/admin\/terms\/[^/]+\/calendar-entries$/, () => json(200, [])],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Takvim" }));

  expect(await screen.findByText("Bu dönemde takvim kaydı yok.")).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Takvimi kapat" }));
  expect(screen.queryByText("Bu dönemde takvim kaydı yok.")).not.toBeInTheDocument();
});
