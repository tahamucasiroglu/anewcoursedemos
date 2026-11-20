import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { adminFake, branchA, branchB, classroom, json, levelFifth, levelPrep, termAutumn, termSpring, type Route } from "./adminFake";

// Spec 0008 AC-12 (web, Sınıflar sekmesi): dönem seçimi (varsayılan güncel) ve şube/durum süzgeci, liste durumları iki
// temada, "pasif" ve "şube pasif" etiketleri, dönem yokken "Önce bir dönem ekleyin", etkin seviye yokken pasif "Sınıf
// ekle" + neden, formda pasif şube/seviye seçenek değil, alan hataları istek göndermeden, sunucu hatası, onaylı pasifleştirme.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

const inactiveBranch = { ...branchB, id: "44444444-4444-4444-8444-444444444445", name: "Kapalı Şube", isActive: false };

async function openTab(routes: Route[], meta: { terms?: unknown[]; levels?: unknown[] } = {}) {
  const api = adminFake([
    ...routes,
    ["GET", /^\/api\/admin\/terms$/, () => json(200, meta.terms ?? [termAutumn, termSpring])],
    ["GET", /^\/api\/admin\/grade-levels$/, () => json(200, meta.levels ?? [levelPrep, { ...levelFifth, isActive: false }])],
    ["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA, branchB, inactiveBranch])],
  ]);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/yonetim";
  render(<App />);
  await userEvent.click(await screen.findByRole("tab", { name: "Sınıflar" }));
  return api;
}

describe.each(["light", "dark"] as const)("%s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("yükleniyor → hata + tekrar dene → güncel dönemin listesi; pasif ve şube pasif etiketleri", async () => {
    let calls = 0;
    const items = [
      classroom({ branchId: branchB.id, branchName: "Avcılar", name: "5-A" }),
      classroom({ id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2", name: "5-B", isActive: false }),
      classroom({ id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3", branchId: inactiveBranch.id, branchName: inactiveBranch.name, branchIsActive: false, name: "5-K" }),
    ];
    await openTab([["GET", /^\/api\/admin\/classrooms$/, () => (++calls === 1 ? json(500, {}) : json(200, { termId: termAutumn.id, items }))]]);

    expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
    await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));

    const rows = await screen.findAllByRole("listitem");
    expect(rows[0]).toHaveTextContent("Avcılar · 5. sınıf · 5-A");
    expect(rows[1]).toHaveTextContent("Merkez · 5. sınıf · 5-B pasif");
    expect(rows[2]).toHaveTextContent("Kapalı Şube · 5. sınıf · 5-K şube pasif");
    expect(screen.getByLabelText("Dönem")).toHaveValue(termAutumn.id);
  });

  it("dönem yoksa 'Önce bir dönem ekleyin' ve sınıf isteği gitmez", async () => {
    const api = await openTab([], { terms: [] });
    expect(await screen.findByText("Önce bir dönem ekleyin")).toBeInTheDocument();
    expect(api.sent("GET", /^\/api\/admin\/classrooms$/)).toHaveLength(0);
  });
});

it("sınıfı olmayan dönemde 'Bu dönemde sınıf yok'; dönem, şube ve durum süzgeci isteğe yansır", async () => {
  const api = await openTab([["GET", /^\/api\/admin\/classrooms$/, (request) => {
    const termId = new URL(request.url).searchParams.get("termId");
    return json(200, { termId: termId ?? termAutumn.id, items: termId === termSpring.id ? [] : [classroom()] });
  }]]);
  await screen.findByText("5-A");

  await userEvent.selectOptions(screen.getByLabelText("Dönem"), termSpring.id);
  expect(await screen.findByText("Bu dönemde sınıf yok")).toBeInTheDocument();
  await userEvent.selectOptions(screen.getByLabelText("Şube"), "Avcılar");
  await userEvent.selectOptions(screen.getByLabelText("Durum"), "Pasif");

  await waitFor(() => {
    const last = new URL(api.sent("GET", /^\/api\/admin\/classrooms$/).at(-1)!.url);
    expect(last.searchParams.get("termId")).toBe(termSpring.id);
    expect(last.searchParams.get("branchId")).toBe(branchB.id);
    expect(last.searchParams.get("active")).toBe("false");
  });
});

it("etkin seviye yoksa 'Sınıf ekle' pasif ve nedeni yazılı", async () => {
  await openTab([["GET", /^\/api\/admin\/classrooms$/, () => json(200, { termId: termAutumn.id, items: [] })]], { levels: [{ ...levelFifth, isActive: false }] });

  const add = await screen.findByRole("button", { name: "Sınıf ekle" });
  expect(add).toBeDisabled();
  expect(add).toHaveAccessibleDescription("Önce bir sınıf seviyesi ekleyin");
});

it("sınıf ekle: pasif şube ve seviye seçenek değil; alan hataları istek göndermeden; sunucu hatası; başarıda seçili döneme eklenir", async () => {
  let items: unknown[] = [];
  const api = await openTab([
    ["GET", /^\/api\/admin\/classrooms$/, () => json(200, { termId: termAutumn.id, items })],
    ["POST", /^\/api\/admin\/classrooms$/, async (request) => {
      const body = (await request.json()) as { termId: string; name: string };
      if (body.name === "5-a") {
        return json(400, { detail: "Bu adla bir sınıf zaten var" });
      }

      items = [classroom({ name: body.name, gradeLevelName: levelPrep.name })];
      return json(200, items[0]);
    }],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Sınıf ekle" }));
  const form = screen.getByRole("form", { name: "Sınıf ekle" });
  const branchOptions = within(within(form).getByLabelText("Şube")).getAllByRole("option").map((o) => o.textContent);
  expect(branchOptions).toEqual(["Seçin", "Merkez", "Avcılar"]);
  const levelOptions = within(within(form).getByLabelText("Sınıf seviyesi")).getAllByRole("option").map((o) => o.textContent);
  expect(levelOptions).toEqual(["Seçin", "Hazırlık"]);

  await userEvent.click(within(form).getByRole("button", { name: "Kaydet" }));
  expect(within(form).getByLabelText("Şube")).toHaveAccessibleDescription("Şube seçin");
  expect(within(form).getByLabelText("Sınıf seviyesi")).toHaveAccessibleDescription("Sınıf seviyesi seçin");
  expect(within(form).getByLabelText("Sınıf adı")).toHaveAccessibleDescription("Sınıf adı zorunludur");
  await userEvent.selectOptions(within(form).getByLabelText("Şube"), "Merkez");
  await userEvent.selectOptions(within(form).getByLabelText("Sınıf seviyesi"), "Hazırlık");
  await userEvent.type(within(form).getByLabelText("Sınıf adı"), "x".repeat(31));
  await userEvent.click(within(form).getByRole("button", { name: "Kaydet" }));
  expect(within(form).getByLabelText("Sınıf adı")).toHaveAccessibleDescription("En fazla 30 karakter olabilir");
  expect(api.sent("POST", /classrooms$/)).toHaveLength(0);

  await userEvent.clear(within(form).getByLabelText("Sınıf adı"));
  await userEvent.type(within(form).getByLabelText("Sınıf adı"), "5-a");
  await userEvent.click(within(form).getByRole("button", { name: "Kaydet" }));
  expect(await within(form).findByText("Bu adla bir sınıf zaten var")).toHaveAttribute("role", "alert");

  await userEvent.clear(within(form).getByLabelText("Sınıf adı"));
  await userEvent.type(within(form).getByLabelText("Sınıf adı"), "H-A{Enter}");
  expect(await screen.findByText("H-A")).toBeInTheDocument();
  const sent = (await api.sent("POST", /classrooms$/).at(-1)!.json()) as { termId: string; branchId: string };
  expect(sent.termId).toBe(termAutumn.id);
  expect(sent.branchId).toBe(branchA.id);
});

it("pasifleştir onay ister; Vazgeç istek göndermez, Onayla gönderir", async () => {
  const api = await openTab([
    ["GET", /^\/api\/admin\/classrooms$/, () => json(200, { termId: termAutumn.id, items: [classroom()] })],
    ["POST", /^\/api\/admin\/classrooms\/[^/]+\/deactivate$/, () => json(200, classroom({ isActive: false }))],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Pasifleştir" }));
  expect(screen.getByRole("dialog")).toHaveTextContent("Sınıf pasifleştirilsin mi?");
  await userEvent.click(screen.getByRole("button", { name: "Vazgeç" }));
  expect(api.sent("POST", /deactivate$/)).toHaveLength(0);

  await userEvent.click(screen.getByRole("button", { name: "Pasifleştir" }));
  await userEvent.click(screen.getByRole("button", { name: "Onayla" }));
  await waitFor(() => expect(api.sent("POST", /deactivate$/)).toHaveLength(1));
});

// Review tur 1, M-1: aynı yerde çizilen form düzenlenen kayda bağlı anahtarla yeniden kurulur — bir satırdan ötekine
// geçince eski değerler kalmaz, "Sınıf ekle" düzenleme formunu boşaltır.
const roomB = classroom({ id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaab2", gradeLevelId: levelPrep.id, gradeLevelName: levelPrep.name, name: "H-B" });

it("5-A'yı düzenle → H-B'yi düzenle → form H-B'nin değerlerini gösterir ve PUT H-B'ye gider", async () => {
  const api = await openTab(
    [
      ["GET", /^\/api\/admin\/classrooms$/, () => json(200, { termId: termAutumn.id, items: [classroom(), roomB] })],
      ["PUT", /^\/api\/admin\/classrooms\/[^/]+$/, () => json(200, roomB)],
    ],
    { levels: [levelPrep, levelFifth] },
  );
  await screen.findByText("H-B");
  await userEvent.click(screen.getAllByRole("button", { name: "Düzenle" })[0]);
  expect(within(screen.getByRole("form", { name: "Düzenle" })).getByLabelText("Sınıf adı")).toHaveValue("5-A");

  await userEvent.click(screen.getAllByRole("button", { name: "Düzenle" })[1]);
  const form = screen.getByRole("form", { name: "Düzenle" });
  expect(within(form).getByLabelText("Sınıf adı")).toHaveValue("H-B");
  expect(within(form).getByLabelText("Sınıf seviyesi")).toHaveValue(levelPrep.id);

  await userEvent.type(within(form).getByLabelText("Sınıf adı"), "2");
  await userEvent.click(within(form).getByRole("button", { name: "Kaydet" }));
  await waitFor(() => expect(api.sent("PUT", /classrooms\/[^/]+$/)).toHaveLength(1));
  const put = api.sent("PUT", /classrooms\/[^/]+$/)[0];
  expect(new URL(put.url, "http://x").pathname).toBe(`/api/admin/classrooms/${roomB.id}`);
  expect(((await put.json()) as { name: string; gradeLevelId: string })).toMatchObject({ name: "H-B2", gradeLevelId: levelPrep.id });
});

it("düzenlerken 'Sınıf ekle' → form boş", async () => {
  await openTab([["GET", /^\/api\/admin\/classrooms$/, () => json(200, { termId: termAutumn.id, items: [classroom()] })]], { levels: [levelPrep, levelFifth] });
  await userEvent.click(await screen.findByRole("button", { name: "Düzenle" }));
  expect(within(screen.getByRole("form", { name: "Düzenle" })).getByLabelText("Sınıf adı")).toHaveValue("5-A");

  await userEvent.click(screen.getByRole("button", { name: "Sınıf ekle" }));
  const form = screen.getByRole("form", { name: "Sınıf ekle" });
  expect(within(form).getByLabelText("Sınıf adı")).toHaveValue("");
  expect(within(form).getByLabelText("Sınıf seviyesi")).toHaveValue("");
  expect(within(form).getByLabelText("Şube")).toHaveValue("");
});
