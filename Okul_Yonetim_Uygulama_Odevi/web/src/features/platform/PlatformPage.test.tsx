import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { json, platformFake, schoolA, schoolB, schools, type Route } from "./platformFake";

// Spec 0007 AC-6..AC-9, AC-13 (web): okul listesi (yükleniyor / boş / dolu / hata + tekrar dene, arama, süzgeç, sayfa),
// okul ekle (alan hataları, gönderiliyor, sunucu hatası, geçici şifre bir kez, var olan hesap), pasifleştir onayı;
// yetkisiz kişide "erişiminiz yok".
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

const queryOf = (r: Request) => new URL(r.url, "http://x").searchParams;

function open(routes: Route[], isPlatformAdmin = true) {
  const api = platformFake(routes, isPlatformAdmin);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/platform";
  render(<App />);
  return api;
}

describe.each(["light", "dark"] as const)("Platform paneli — %s tema", (mode) => {
  beforeEach(() => {
    window.localStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(document.documentElement.getAttribute("data-theme")).toBe(mode);
  });

  it("liste: özet bilgiler ve pasif etiketi; arama/süzgeç sorguya gider; ikinci sayfa istenir; boş sonuç mesajı", async () => {
    const api = open([["GET", /^\/api\/platform\/schools$/, (request) => {
      const q = queryOf(request);
      if (q.get("search") === "yok") {
        return json(200, schools([]));
      }

      return json(200, schools([schoolA, schoolB], 30, Number(q.get("page") ?? 1)));
    }]]);

    expect(await screen.findByRole("heading", { name: "Platform paneli" })).toBeInTheDocument();
    expect(await screen.findByText("Çınar Koleji")).toBeInTheDocument();
    expect(screen.getAllByText(/Etkin şube: 2 · Etkin kişi: 14/)).toHaveLength(2);
    expect(screen.getByText("Kurucu: Ayşe Kurucu (ayse@ornek.test)")).toBeInTheDocument();
    expect(screen.getByText("pasif")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Işık Okulu — Etkinleştir" })).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Sonraki" }));
    await waitFor(() => expect(api.sent("GET", /^\/api\/platform\/schools$/).some((r) => queryOf(r).get("page") === "2")).toBe(true));
    await userEvent.selectOptions(await screen.findByLabelText("Durum"), "inactive");
    await waitFor(() => expect(api.sent("GET", /^\/api\/platform\/schools$/).some((r) => queryOf(r).get("status") === "inactive")).toBe(true));
    await userEvent.type(screen.getByLabelText("Okul adı ara"), "yok");
    expect(await screen.findByText("Aramaya uyan okul yok")).toBeInTheDocument();
  });
});

it("hiç okul yokken 'Henüz okul yok'", async () => {
  open([["GET", /^\/api\/platform\/schools$/, () => json(200, schools([]))]]);
  expect(await screen.findByText("Henüz okul yok")).toBeInTheDocument();
});

it("liste alınamazsa hata duyurulur; tekrar dene yeniden ister", async () => {
  let calls = 0;
  open([["GET", /^\/api\/platform\/schools$/, () => (++calls === 1 ? json(500, { detail: "Beklenmeyen hata" }) : json(200, schools([schoolA])))]]);
  expect(await screen.findByRole("alert")).toHaveTextContent("Beklenmeyen hata");
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByText("Çınar Koleji")).toBeInTheDocument();
});

it("okul ekle: alan hataları istek göndermez; gönderilirken kilitli; 'Bu okul zaten var'; sonra geçici şifre bir kez", async () => {
  let posts = 0;
  let release: () => void = () => {};
  const duplicate = new Promise<Response>((resolve) => {
    release = () => resolve(json(400, { detail: "Bu okul zaten var" }));
  });
  const api = open([
    ["GET", /^\/api\/platform\/schools$/, () => json(200, schools([schoolA]))],
    ["POST", /^\/api\/platform\/schools$/, () => {
      posts++;
      if (posts === 1) {
        return duplicate;
      }

      return json(200, { tenantId: schoolA.tenantId, existingAccount: false, temporaryPassword: "Ab3dEf7hJk9m" });
    }],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Okul ekle" }));
  await userEvent.type(screen.getByLabelText("Kurucu e-posta"), "gecersiz");
  await userEvent.click(screen.getByRole("button", { name: "Okulu aç" }));

  expect(screen.getByText("Okul adı zorunludur")).toHaveAttribute("role", "alert");
  expect(screen.getByText("Şube adı zorunludur")).toBeInTheDocument();
  expect(screen.getByText("Ad soyad zorunludur")).toBeInTheDocument();
  expect(screen.getByText("Geçerli bir e-posta girin")).toBeInTheDocument();
  expect(screen.getByLabelText("Okul adı")).toHaveAttribute("aria-invalid", "true");
  expect(api.sent("POST", /^\/api\/platform\/schools$/)).toHaveLength(0);

  await userEvent.type(screen.getByLabelText("Okul adı"), "Yeni Koleji");
  await userEvent.type(screen.getByLabelText("İlk şube adı"), "Merkez");
  await userEvent.type(screen.getByLabelText("Kurucu ad soyad"), "Veli Kurucu");
  await userEvent.clear(screen.getByLabelText("Kurucu e-posta"));
  await userEvent.type(screen.getByLabelText("Kurucu e-posta"), "veli@ornek.test");
  await userEvent.click(screen.getByRole("button", { name: "Okulu aç" }));
  expect(await screen.findByRole("button", { name: "Açılıyor…" })).toBeDisabled();
  release();
  expect(await screen.findByText("Bu okul zaten var")).toHaveAttribute("role", "alert");

  await userEvent.click(screen.getByRole("button", { name: "Okulu aç" }));
  expect(await screen.findByText("Ab3dEf7hJk9m")).toBeInTheDocument();
  expect(await api.sent("POST", /^\/api\/platform\/schools$/)[0].json()).toEqual({ schoolName: "Yeni Koleji", branchName: "Merkez", founderName: "Veli Kurucu", founderEmail: "veli@ornek.test" });
  await userEvent.click(screen.getByRole("button", { name: "Tamam" }));
  expect(await screen.findByText("Çınar Koleji")).toBeInTheDocument();
  expect(screen.queryByText("Ab3dEf7hJk9m")).not.toBeInTheDocument();
});

it("var olan kişi: 'zaten hesabı var' mesajı, şifre gösterilmez", async () => {
  open([
    ["GET", /^\/api\/platform\/schools$/, () => json(200, schools([]))],
    ["POST", /^\/api\/platform\/schools$/, () => json(200, { tenantId: schoolA.tenantId, existingAccount: true, temporaryPassword: null })],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Okul ekle" }));
  await userEvent.type(screen.getByLabelText("Okul adı"), "İkinci Okul");
  await userEvent.type(screen.getByLabelText("İlk şube adı"), "Merkez");
  await userEvent.type(screen.getByLabelText("Kurucu ad soyad"), "Ayşe Kurucu");
  await userEvent.type(screen.getByLabelText("Kurucu e-posta"), "ayse@ornek.test");
  await userEvent.click(screen.getByRole("button", { name: "Okulu aç" }));

  expect(await screen.findByText("Bu kişinin zaten bir hesabı var; mevcut şifresiyle girer.")).toHaveAttribute("role", "status");
});

it("pasifleştir: onay iletişimi; Vazgeç istek göndermez; Onayla sonra liste yenilenir", async () => {
  let deactivated = false;
  const api = open([
    ["GET", /^\/api\/platform\/schools$/, () => json(200, schools([{ ...schoolA, isActive: !deactivated }]))],
    ["POST", /^\/api\/platform\/schools\/[^/]+\/deactivate$/, () => {
      deactivated = true;
      return json(200, { ...schoolA, isActive: false });
    }],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Çınar Koleji — Pasifleştir" }));
  const dialog = screen.getByRole("dialog");
  expect(dialog).toHaveTextContent("Okul pasifleştirilsin mi?");
  await userEvent.click(within(dialog).getByRole("button", { name: "Vazgeç" }));
  expect(api.sent("POST", /deactivate$/)).toHaveLength(0);

  await userEvent.click(screen.getByRole("button", { name: "Çınar Koleji — Pasifleştir" }));
  await userEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Onayla" }));
  expect(await screen.findByRole("button", { name: "Çınar Koleji — Etkinleştir" })).toBeInTheDocument();
  expect(api.sent("POST", /deactivate$/)).toHaveLength(1);
});

it("platform yöneticisi olmayan kişi: 'Bu sayfaya erişiminiz yok.' ve hiçbir platform isteği gitmez", async () => {
  const api = open([], false);
  expect(await screen.findByRole("alert")).toHaveTextContent("Bu sayfaya erişiminiz yok.");
  expect(api.requests.some((r) => r.url.includes("/api/platform/"))).toBe(false);
});
