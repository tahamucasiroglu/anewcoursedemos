import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { adminFake, branchA, branchB, json, page, type Route } from "../admin/adminFake";
import { classroomA, guardianView, siblingId, studentDetail, studentRow } from "./studentsFake";

// Spec 0009 AC-13 (web, öğrenci kartı): bölümler ve boş durumları ("Sınıf atanmamış", "Henüz veli yok", "Kardeş yok",
// "Henüz kimse eklenmedi", "Sağlık bilgisi şu anda gösterilemiyor."), "Ayrıldı" etiketi, sınırda pasif ekle düğmesi +
// neden (4 veli / 10 kişi), veli ekleme sonucu (geçici şifre: uyarı, kopyala, kopyalandı / "zaten hesabı var"), onay
// iletişimi (Esc kapatır), alan hataları istek göndermeden, kardeş satırı o kardeşin kartını açar, teslim kişisi formu
// düzenlenen satıra bağlı (T15) — iki temada.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

async function openCard(detail: Record<string, unknown>, routes: Route[] = []) {
  const api = adminFake([
    ...routes,
    ["GET", /^\/api\/admin\/students$/, () => json(200, page([studentRow()]))],
    ["GET", /^\/api\/admin\/students\/[^/]+$/, (_request, match) =>
      json(200, match[0].endsWith(siblingId) ? studentDetail({ id: siblingId, fullName: "Can Ak", siblings: [] }) : studentDetail(detail))],
    ["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA, branchB])],
    ["GET", /^\/api\/classrooms$/, () => json(200, [classroomA])],
  ]);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/ogrenciler";
  render(<App />);
  await userEvent.click(await screen.findByRole("button", { name: "Zeynep Ak — Aç" }));
  await screen.findByRole("heading", { level: 3 });
  return api;
}

const fourGuardians = [0, 1, 2, 3].map((i) => guardianView({ userId: `cccccccc-cccc-4ccc-8ccc-ccccccccccd${i}`, fullName: `Veli ${i}` }));
const tenPeople = Array.from({ length: 10 }, (_, i) => ({ fullName: `Kişi ${i}`, relationship: "Komşu", phone: `+9053200000${String(i).padStart(2, "0")}` }));

describe.each(["light", "dark"] as const)("Öğrenci kartı — %s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("dolu kart: bilgiler, sağlık notu ve alerji ayrı başlıkla, veliler, kardeşler, teslim kişileri", async () => {
    await openCard({});

    expect(screen.getByRole("heading", { level: 3, name: "Zeynep Ak" })).toBeInTheDocument();
    expect(screen.getByText("04.05.2018")).toBeInTheDocument();
    expect(screen.getByText("3-A")).toBeInTheDocument();
    expect(screen.getByText("Sağlık notu").nextElementSibling).toHaveTextContent("Astım");
    expect(screen.getByText("Alerji").nextElementSibling).toHaveTextContent("Fıstık");
    expect(screen.getByText("Ayla Ak")).toBeInTheDocument();
    expect(screen.getByText("(Anne)")).toBeInTheDocument();
    expect(screen.getByText("Can Ak")).toBeInTheDocument();
    expect(screen.getByText("Hasan Ak")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Veli ekle" })).toBeEnabled();
    expect(screen.queryByText("Ayrıldı")).not.toBeInTheDocument();
  });

  it("boş durumlar, sağlık gösterilemiyor ve Ayrıldı etiketi", async () => {
    await openCard({ classroomId: null, classroomName: null, guardians: [], siblings: [], pickupPersons: [], isActive: false, health: { available: false, healthNote: null, allergy: null } });

    expect(screen.getByRole("heading", { level: 3 })).toHaveTextContent("Zeynep Ak Ayrıldı");
    expect(screen.getByText("Sınıf atanmamış")).toBeInTheDocument();
    expect(screen.getByText("Sağlık bilgisi şu anda gösterilemiyor.")).toBeInTheDocument();
    expect(screen.getByText("Henüz veli yok")).toBeInTheDocument();
    expect(screen.getByText("Kardeş yok")).toBeInTheDocument();
    expect(screen.getByText("Henüz kimse eklenmedi")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Geri al" })).toBeInTheDocument();
  });

  it("sınırda: 4 velide 'Veli ekle', 10 kişide 'Kişi ekle' pasif ve nedeni yazılı", async () => {
    await openCard({ guardians: fourGuardians, pickupPersons: tenPeople });

    expect(screen.getByRole("button", { name: "Veli ekle" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Veli ekle" })).toHaveAccessibleDescription("Bir öğrencinin en fazla 4 velisi olabilir.");
    expect(screen.getByRole("button", { name: "Kişi ekle" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Kişi ekle" })).toHaveAccessibleDescription("En fazla 10 kişi eklenebilir.");
  });

  it("yeni veli: alan hataları istek göndermez; sonra geçici şifre bir kez, uyarı, kopyala → kopyalandı", async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
    const api = await openCard({}, [["POST", /\/guardians$/, () => json(200, { userId: "dddddddd-dddd-4ddd-8ddd-ddddddddddd1", existingAccount: false, temporaryPassword: "Zx8cVb7nMq2w" })]]);

    await userEvent.click(screen.getByRole("button", { name: "Veli ekle" }));
    const form = screen.getByRole("form", { name: "Veli ekle" });
    await userEvent.type(within(form).getByLabelText("Telefon"), "123");
    await userEvent.click(within(form).getByRole("button", { name: "Kaydet" }));
    expect(within(form).getByLabelText("Ad soyad")).toHaveAccessibleDescription("Ad soyad zorunludur");
    expect(within(form).getByLabelText("E-posta")).toHaveAccessibleDescription("E-posta zorunludur");
    expect(within(form).getByLabelText("Telefon")).toHaveAccessibleDescription("Geçerli bir telefon numarası girin");
    expect(within(form).getByLabelText("Yakınlık")).toHaveAccessibleDescription("Yakınlık seçin");
    expect(api.sent("POST", /\/guardians$/)).toHaveLength(0);

    await userEvent.type(within(form).getByLabelText("Ad soyad"), "Yeni Veli");
    await userEvent.type(within(form).getByLabelText("E-posta"), "yeni@ornek.test");
    await userEvent.clear(within(form).getByLabelText("Telefon"));
    await userEvent.selectOptions(within(form).getByLabelText("Yakınlık"), "Father");
    await userEvent.click(within(form).getByRole("button", { name: "Kaydet" }));

    expect(await screen.findByText("Zx8cVb7nMq2w")).toBeInTheDocument();
    expect(screen.getByText("Bu şifre bir daha gösterilmeyecek. Kişiye siz iletin.")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Kopyala" }));
    expect(await screen.findByText("Kopyalandı")).toHaveAttribute("role", "status");
    expect(await api.sent("POST", /\/guardians$/)[0].json()).toEqual({ fullName: "Yeni Veli", email: "yeni@ornek.test", phone: null, relationship: "Father" });
  });
});

it("var olan kişi: 'Bu kişinin zaten bir hesabı var; mevcut şifresiyle girer.'", async () => {
  await openCard({}, [["POST", /\/guardians$/, () => json(200, { userId: "dddddddd-dddd-4ddd-8ddd-ddddddddddd1", existingAccount: true, temporaryPassword: null })]]);

  await userEvent.click(screen.getByRole("button", { name: "Veli ekle" }));
  const form = screen.getByRole("form", { name: "Veli ekle" });
  await userEvent.type(within(form).getByLabelText("Ad soyad"), "Var Olan");
  await userEvent.type(within(form).getByLabelText("E-posta"), "var@ornek.test");
  await userEvent.selectOptions(within(form).getByLabelText("Yakınlık"), "Other");
  await userEvent.click(within(form).getByRole("button", { name: "Kaydet" }));

  expect(await screen.findByText("Bu kişinin zaten bir hesabı var; mevcut şifresiyle girer.")).toHaveAttribute("role", "status");
});

it("veli kaldırma onaylı: Esc kapatır ve istek gitmez; Onayla DELETE gönderir", async () => {
  const api = await openCard({}, [["DELETE", /\/guardians\/[^/]+$/, () => json(200, { id: "x" })]]);

  await userEvent.click(screen.getByRole("button", { name: "Ayla Ak — Kaldır" }));
  expect(screen.getByRole("dialog")).toHaveTextContent("Bu velinin öğrenciyle bağı kaldırılsın mı?");
  await userEvent.keyboard("{Escape}");
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  expect(api.sent("DELETE", /\/guardians\//)).toHaveLength(0);

  await userEvent.click(screen.getByRole("button", { name: "Ayla Ak — Kaldır" }));
  await userEvent.click(screen.getByRole("button", { name: "Onayla" }));
  await waitFor(() => expect(api.sent("DELETE", /\/guardians\/cccccccc-cccc-4ccc-8ccc-ccccccccccc1$/)).toHaveLength(1));
});

it("ayrıldı olarak işaretleme onaylıdır", async () => {
  const api = await openCard({}, [["POST", /\/deactivate$/, () => json(200, { id: "x" })]]);

  await userEvent.click(screen.getByRole("button", { name: "Ayrıldı olarak işaretle" }));
  expect(screen.getByRole("dialog")).toHaveTextContent("ayrıldı olarak işaretlensin mi?");
  await userEvent.click(screen.getByRole("button", { name: "Onayla" }));

  await waitFor(() => expect(api.sent("POST", /\/deactivate$/)).toHaveLength(1));
});

it("kardeş satırı o kardeşin kartını açar", async () => {
  await openCard({});

  await userEvent.click(screen.getByRole("button", { name: "Can Ak — Aç" }));

  expect(await screen.findByRole("heading", { level: 3, name: "Can Ak" })).toBeInTheDocument();
});

it("teslim kişisi: X'i düzenle → Y'yi düzenle form Y'nin değerleriyle; kaydet bütün listeyi gönderir (T15)", async () => {
  const api = await openCard(
    { pickupPersons: [{ fullName: "Hasan Ak", relationship: "Dede", phone: "+905320001122" }, { fullName: "Ayla Teyze", relationship: "Teyze", phone: "+902125556677" }] },
    [["PUT", /\/pickup-persons$/, () => json(200, { id: "x" })]],
  );

  await userEvent.click(screen.getByRole("button", { name: "Hasan Ak — Düzenle" }));
  expect(screen.getByLabelText("Yakınlık (ör. dede, teyze)")).toHaveValue("Dede");
  await userEvent.click(screen.getByRole("button", { name: "Ayla Teyze — Düzenle" }));
  expect(screen.getByLabelText("Ad soyad")).toHaveValue("Ayla Teyze");
  expect(screen.getByLabelText("Yakınlık (ör. dede, teyze)")).toHaveValue("Teyze");
  await userEvent.clear(screen.getByLabelText("Yakınlık (ör. dede, teyze)"));
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByLabelText("Yakınlık (ör. dede, teyze)")).toHaveAccessibleDescription("Yakınlık seçin");
  expect(api.sent("PUT", /\/pickup-persons$/)).toHaveLength(0);

  await userEvent.type(screen.getByLabelText("Yakınlık (ör. dede, teyze)"), "Hala");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));

  await waitFor(() => expect(api.sent("PUT", /\/pickup-persons$/)).toHaveLength(1));
  expect(await api.sent("PUT", /\/pickup-persons$/)[0].json()).toEqual({
    persons: [
      { fullName: "Hasan Ak", relationship: "Dede", phone: "+905320001122" },
      { fullName: "Ayla Teyze", relationship: "Hala", phone: "+902125556677" },
    ],
  });
});

it("sağlık düzenleme: sunucu hatası duyurulur", async () => {
  await openCard({}, [["PUT", /\/health$/, () => json(400, { detail: "Sağlık bilgisi şu anda kaydedilemiyor." })]]);

  await userEvent.click(screen.getAllByRole("button", { name: "Düzenle" })[1]);
  await userEvent.clear(screen.getByLabelText("Alerji"));
  await userEvent.type(screen.getByLabelText("Alerji"), "Polen");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Sağlık bilgisi şu anda kaydedilemiyor.");
});
