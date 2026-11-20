import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { adminFake, branchA, json, levelFifth, levelPrep, type Route } from "./adminFake";

// Spec 0008 AC-12 (web, Sınıf seviyeleri sekmesi): liste durumları iki temada, form alan hataları istek göndermeden,
// gönderiliyor, sunucu hatası, pasifleştirme onayı (Esc, odak), pasif etiketi, klavyeyle kayıt.
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
  await userEvent.click(await screen.findByRole("tab", { name: "Sınıf seviyeleri" }));
  return api;
}

describe.each(["light", "dark"] as const)("%s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("yükleniyor → hata + tekrar dene → sıralı liste; pasif seviye etiketli", async () => {
    let calls = 0;
    await openTab([["GET", /^\/api\/admin\/grade-levels$/, () => (++calls === 1 ? json(500, {}) : json(200, [levelPrep, { ...levelFifth, isActive: false }]))]]);

    expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
    await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));

    const rows = await screen.findAllByRole("listitem");
    expect(rows[0]).toHaveTextContent("Sıra 1 · Hazırlık");
    expect(rows[1]).toHaveTextContent("Sıra 5 · 5. sınıf pasif");
    expect(screen.getByRole("button", { name: "Etkinleştir" })).toBeInTheDocument();
  });

  it("boş listede 'Henüz sınıf seviyesi yok'", async () => {
    await openTab([["GET", /^\/api\/admin\/grade-levels$/, () => json(200, [])]]);
    expect(await screen.findByText("Henüz sınıf seviyesi yok")).toBeInTheDocument();
  });
});

it("seviye ekle: varsayılan sıra en büyük + 1; alan hataları istek göndermeden; sunucu hatası duyurulur; Enter ile kaydedilir", async () => {
  let levels = [levelPrep, levelFifth];
  const api = await openTab([
    ["GET", /^\/api\/admin\/grade-levels$/, () => json(200, levels)],
    ["POST", /^\/api\/admin\/grade-levels$/, async (request) => {
      const body = (await request.json()) as { name: string; sortOrder: number };
      if (body.name === "HAZIRLIK") {
        return json(400, { detail: "Bu adla bir seviye zaten var" });
      }

      const created = { id: "77777777-7777-4777-8777-777777777776", name: body.name, sortOrder: body.sortOrder, isActive: true };
      levels = [...levels, created];
      return json(200, created);
    }],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Seviye ekle" }));
  expect(screen.getByLabelText("Sıra")).toHaveValue("6");

  await userEvent.clear(screen.getByLabelText("Sıra"));
  await userEvent.type(screen.getByLabelText("Sıra"), "0");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByText("Seviye adı zorunludur")).toHaveAttribute("role", "alert");
  expect(screen.getByLabelText("Seviye adı")).toHaveAccessibleDescription("Seviye adı zorunludur");
  expect(screen.getByLabelText("Sıra")).toHaveAccessibleDescription("Sıra 1 ile 99 arasında bir tam sayı olmalı");
  expect(api.sent("POST", /grade-levels$/)).toHaveLength(0);

  await userEvent.clear(screen.getByLabelText("Sıra"));
  await userEvent.type(screen.getByLabelText("Sıra"), "6");
  await userEvent.type(screen.getByLabelText("Seviye adı"), "x".repeat(51));
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByText("En fazla 50 karakter olabilir")).toBeInTheDocument();
  expect(api.sent("POST", /grade-levels$/)).toHaveLength(0);

  await userEvent.clear(screen.getByLabelText("Seviye adı"));
  await userEvent.type(screen.getByLabelText("Seviye adı"), "HAZIRLIK");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Bu adla bir seviye zaten var")).toHaveAttribute("role", "alert");

  await userEvent.clear(screen.getByLabelText("Seviye adı"));
  await userEvent.type(screen.getByLabelText("Seviye adı"), "6. sınıf{Enter}");
  expect(await screen.findByText("6. sınıf")).toBeInTheDocument();
  expect(screen.queryByLabelText("Seviye adı")).not.toBeInTheDocument();
});

it("gönderilirken Kaydet kilitli ve 'Kaydediliyor…' yazar", async () => {
  await openTab([
    ["GET", /^\/api\/admin\/grade-levels$/, () => json(200, [levelPrep])],
    ["PUT", /^\/api\/admin\/grade-levels\/[^/]+$/, () => new Promise<Response>(() => {})],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Düzenle" }));
  expect(screen.getByLabelText("Seviye adı")).toHaveValue("Hazırlık");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));

  expect(await screen.findByRole("button", { name: "Kaydediliyor…" })).toBeDisabled();
});

it("pasifleştir onay ister: Esc kapatır ve odak düğmeye döner (istek yok); Onayla gönderir", async () => {
  let levels = [levelPrep];
  const api = await openTab([
    ["GET", /^\/api\/admin\/grade-levels$/, () => json(200, levels)],
    ["POST", /^\/api\/admin\/grade-levels\/[^/]+\/deactivate$/, () => {
      levels = [{ ...levelPrep, isActive: false }];
      return json(200, levels[0]);
    }],
  ]);
  const deactivate = await screen.findByRole("button", { name: "Pasifleştir" });

  await userEvent.click(deactivate);
  expect(screen.getByRole("dialog")).toHaveTextContent("Seviye pasifleştirilsin mi?");
  expect(screen.getByRole("button", { name: "Vazgeç" })).toHaveFocus();
  await userEvent.keyboard("{Escape}");
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  expect(deactivate).toHaveFocus();
  expect(api.sent("POST", /deactivate$/)).toHaveLength(0);

  await userEvent.click(deactivate);
  await userEvent.click(screen.getByRole("button", { name: "Onayla" }));
  await waitFor(() => expect(api.sent("POST", /deactivate$/)).toHaveLength(1));
  expect(await screen.findByText("pasif")).toBeInTheDocument();
});
