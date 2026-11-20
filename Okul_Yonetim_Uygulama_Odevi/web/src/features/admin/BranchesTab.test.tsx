import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { adminFake, branchA, branchB, json, type Route } from "./adminFake";

// Spec 0005 AC-3, AC-4, AC-13 (web): şube listesi durumları, ekleme formu, pasifleştirme onayı, son etkin şube koruması.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
});

function open(routes: Route[]) {
  const api = adminFake(routes);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/yonetim";
  render(<App />);
  return api;
}

it("yükleniyor → hata + tekrar dene → liste", async () => {
  let calls = 0;
  open([["GET", /^\/api\/admin\/branches$/, () => (++calls === 1 ? json(500, {}) : json(200, [branchA]))]]);

  expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));

  expect(await screen.findByText("Merkez")).toBeInTheDocument();
});

it("boş listede 'Henüz şube yok'", async () => {
  open([["GET", /^\/api\/admin\/branches$/, () => json(200, [])]]);
  expect(await screen.findByText("Henüz şube yok")).toBeInTheDocument();
});

it("şube ekle: boş ad ve geçersiz telefon istek göndermeden gösterilir; sunucu hatası duyurulur; başarıda liste yenilenir", async () => {
  let names = [branchA];
  const api = open([
    ["GET", /^\/api\/admin\/branches$/, () => json(200, names)],
    ["POST", /^\/api\/admin\/branches$/, async (request) => {
      const body = (await request.json()) as { name: string };
      if (body.name === "Merkez") {
        return json(400, { detail: "Bu adla bir şube zaten var" });
      }

      names = [...names, { ...branchB, name: body.name }];
      return json(200, names[names.length - 1]);
    }],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Şube ekle" }));

  await userEvent.type(screen.getByLabelText("Telefon"), "123");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByText("Şube adı zorunludur")).toHaveAttribute("role", "alert");
  expect(screen.getByLabelText("Şube adı")).toHaveAccessibleDescription("Şube adı zorunludur");
  expect(screen.getByText("Geçerli bir telefon numarası girin")).toBeInTheDocument();
  expect(api.sent("POST", /^\/api\/admin\/branches$/)).toHaveLength(0);

  await userEvent.clear(screen.getByLabelText("Telefon"));
  await userEvent.type(screen.getByLabelText("Şube adı"), "Merkez");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Bu adla bir şube zaten var")).toHaveAttribute("role", "alert");

  await userEvent.clear(screen.getByLabelText("Şube adı"));
  await userEvent.type(screen.getByLabelText("Şube adı"), "Kadıköy{Enter}");
  expect(await screen.findByText("Kadıköy")).toBeInTheDocument();
  expect(screen.queryByLabelText("Şube adı")).not.toBeInTheDocument();
});

it("gönderilirken Kaydet düğmesi kilitli ve 'Kaydediliyor…' yazar", async () => {
  open([
    ["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA])],
    ["POST", /^\/api\/admin\/branches$/, () => new Promise<Response>(() => {})],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Şube ekle" }));
  await userEvent.type(screen.getByLabelText("Şube adı"), "Yeni");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));

  expect(await screen.findByRole("button", { name: "Kaydediliyor…" })).toBeDisabled();
});

it("pasifleştir onay ister: Esc kapatır (istek yok), Onayla gönderir; pasif şube etiketlenir", async () => {
  let list = [branchA, branchB];
  const api = open([
    ["GET", /^\/api\/admin\/branches$/, () => json(200, list)],
    ["POST", /^\/api\/admin\/branches\/[^/]+\/deactivate$/, () => {
      list = [branchA, { ...branchB, isActive: false }];
      return json(200, list[1]);
    }],
  ]);
  await screen.findByText("Avcılar");
  const deactivate = () => screen.getAllByRole("button", { name: "Pasifleştir" })[1];

  await userEvent.click(deactivate());
  expect(screen.getByRole("dialog")).toHaveTextContent("Şube pasifleştirilsin mi?");
  expect(screen.getByRole("button", { name: "Vazgeç" })).toHaveFocus();
  await userEvent.keyboard("{Escape}");
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  expect(api.sent("POST", /deactivate$/)).toHaveLength(0);

  await userEvent.click(deactivate());
  await userEvent.click(screen.getByRole("button", { name: "Onayla" }));
  await waitFor(() => expect(api.sent("POST", /deactivate$/)).toHaveLength(1));
  expect(await screen.findByText("pasif")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Etkinleştir" })).toBeInTheDocument();
});

it("son etkin şubede Pasifleştir pasif ve nedeni yazılı", async () => {
  open([["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA, { ...branchB, isActive: false }])]]);

  const button = await screen.findByRole("button", { name: "Pasifleştir" });

  expect(button).toBeDisabled();
  expect(button).toHaveAccessibleDescription("Okulun son etkin şubesi pasifleştirilemez");
});
