import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { adminFake, branchA, branchB, json, memberDetail, memberRow, page, teacherId, type Route } from "./adminFake";

// Spec 0005 AC-5, AC-6, AC-7, AC-13 (web): kullanıcı listesi (arama, süzgeç, sayfalama, boş), ekleme formu (alan hataları,
// geçici şifre bir kez + kopyala, var olan hesap mesajı).
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
});

async function openMembers(routes: Route[]) {
  const api = adminFake([["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA, branchB])], ...routes]);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/yonetim";
  render(<App />);
  await userEvent.click(await screen.findByRole("tab", { name: "Kullanıcılar" }));
  return api;
}

const queryOf = (r: Request) => new URL(r.url, "http://x").searchParams;

it("liste: rol ve şube adlarıyla satırlar; arama ve süzgeç sorguya gider; sayfalama ikinci sayfayı ister; boş sonuç mesajı", async () => {
  const api = await openMembers([["GET", /^\/api\/admin\/members$/, (request) => {
    const q = queryOf(request);
    if (q.get("search") === "yok") {
      return json(200, page([]));
    }

    return json(200, page([memberRow()], 30, Number(q.get("page") ?? 1)));
  }]]);

  expect(await screen.findByText("Ali Öğretmen")).toBeInTheDocument();
  expect(screen.getByText("Öğretmen (Merkez)")).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Sonraki" }));
  await waitFor(() => expect(api.sent("GET", /^\/api\/admin\/members$/).some((r) => queryOf(r).get("page") === "2")).toBe(true));

  await userEvent.selectOptions(screen.getByLabelText("Rol"), "Guardian");
  await userEvent.selectOptions(screen.getByLabelText("Durum"), "inactive");
  await userEvent.type(screen.getByLabelText("Ad veya e-posta ara"), "yok");

  expect(await screen.findByText("Aramaya uyan kişi yok")).toBeInTheDocument();
  const last = queryOf(api.sent("GET", /^\/api\/admin\/members$/).at(-1)!);
  expect([last.get("search"), last.get("role"), last.get("status"), last.get("page")]).toEqual(["yok", "Guardian", "inactive", "1"]);
});

it("kullanıcı ekle: alan hataları istek göndermez; yeni kişide geçici şifre bir kez, uyarı ve kopyala → 'Kopyalandı'", async () => {
  const writeText = vi.fn().mockResolvedValue(undefined);
  Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
  const api = await openMembers([
    ["GET", /^\/api\/admin\/members$/, () => json(200, page([]))],
    ["POST", /^\/api\/admin\/members$/, () => json(200, { userId: teacherId, existingAccount: false, temporaryPassword: "Ab3dEf7hJk9m" })],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Kullanıcı ekle" }));

  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByText("Ad soyad zorunludur")).toBeInTheDocument();
  expect(screen.getByText("E-posta zorunludur")).toBeInTheDocument();
  expect(screen.getByText("En az bir rol seçin")).toBeInTheDocument();
  await userEvent.type(screen.getByLabelText("Ad soyad"), "Ali Öğretmen");
  await userEvent.type(screen.getByLabelText("E-posta"), "ali@ornek.test");
  await userEvent.click(screen.getByLabelText("Öğretmen"));
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByText("Bu rol için en az bir şube seçin")).toBeInTheDocument();
  expect(api.sent("POST", /^\/api\/admin\/members$/)).toHaveLength(0);

  await userEvent.click(screen.getByLabelText("Avcılar"));
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));

  expect(await screen.findByText("Ab3dEf7hJk9m")).toBeInTheDocument();
  expect(screen.getByText("Bu şifre bir daha gösterilmeyecek. Kişiye siz iletin.")).toBeInTheDocument();
  const body = (await api.sent("POST", /^\/api\/admin\/members$/)[0].json()) as Record<string, unknown>;
  expect(body).toEqual({ fullName: "Ali Öğretmen", email: "ali@ornek.test", phone: null, roles: [{ role: "Teacher", branchIds: [branchB.id] }] });
  await userEvent.click(screen.getByRole("button", { name: "Kopyala" }));
  expect(writeText).toHaveBeenCalledWith("Ab3dEf7hJk9m");
  expect(await screen.findByText("Kopyalandı")).toHaveAttribute("role", "status");
  await userEvent.click(screen.getByRole("button", { name: "Tamam" }));
  expect(screen.queryByText("Ab3dEf7hJk9m")).not.toBeInTheDocument();
});

it("var olan hesap: şifre gösterilmez, 'zaten hesabı var' mesajı; aynı okulda kayıtlıysa sunucu hatası duyurulur", async () => {
  let attempt = 0;
  await openMembers([
    ["GET", /^\/api\/admin\/members$/, () => json(200, page([]))],
    ["POST", /^\/api\/admin\/members$/, () => (++attempt === 1
      ? json(400, { detail: "Bu kişi bu okulda zaten kayıtlı" })
      : json(200, { userId: teacherId, existingAccount: true, temporaryPassword: null }))],
  ]);
  await userEvent.click(await screen.findByRole("button", { name: "Kullanıcı ekle" }));
  await userEvent.type(screen.getByLabelText("Ad soyad"), "Veli Kişi");
  await userEvent.type(screen.getByLabelText("E-posta"), "veli@ornek.test");
  await userEvent.click(screen.getByLabelText("Kurucu / Yönetici"));

  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(await screen.findByText("Bu kişi bu okulda zaten kayıtlı")).toHaveAttribute("role", "alert");
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));

  expect(await screen.findByText("Bu kişinin zaten bir hesabı var; mevcut şifresiyle girer.")).toBeInTheDocument();
  expect(screen.queryByText("Geçici şifre")).not.toBeInTheDocument();
});

it("liste yüklenemezse hata + tekrar dene", async () => {
  let calls = 0;
  await openMembers([["GET", /^\/api\/admin\/members$/, () => (++calls === 1 ? Promise.reject(new TypeError("Failed to fetch")) : json(200, page([memberRow()])))]]);

  expect(await screen.findByText("Sunucuya ulaşılamıyor. Lütfen daha sonra tekrar deneyin.")).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByText("Ali Öğretmen")).toBeInTheDocument();
  void memberDetail;
});

it("rol hatası rol alanına bağlı (review L-5)", async () => {
  await openMembers([["GET", /^\/api\/admin\/members$/, () => json(200, page([]))]]);
  await userEvent.click(await screen.findByRole("button", { name: "Kullanıcı ekle" }));

  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));

  expect(screen.getByRole("group", { name: "Roller ve şubeler" })).toHaveAccessibleDescription("En az bir rol seçin");
});
