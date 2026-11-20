import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../../App";
import { adminFake, branchA, branchB, founderId, json, memberDetail, memberRow, page, teacherId, type Route } from "./adminFake";

// Spec 0005 AC-8, AC-9, AC-10, AC-11, AC-13 (web): kişi kartı — profil düzenleme, roller, pasifleştirme ve şifre
// sıfırlama (onaylı), korumalarda pasif düğme + neden.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
});

async function openCard(detail: Record<string, unknown>, routes: Route[] = [], activeFounders = 2) {
  const api = adminFake([
    ["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA, branchB])],
    ["GET", /^\/api\/admin\/members$/, (request) =>
      new URL(request.url, "http://x").searchParams.get("role") === "Founder"
        ? json(200, page([], activeFounders))
        : json(200, page([memberRow(detail)]))],
    ["GET", /^\/api\/admin\/members\/[^/]+$/, () => json(200, memberDetail(detail))],
    ...routes,
  ]);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/yonetim";
  render(<App />);
  await userEvent.click(await screen.findByRole("tab", { name: "Kullanıcılar" }));
  await userEvent.click(await screen.findByRole("button", { name: /— Aç$/ }));
  await screen.findByRole("heading", { level: 3 });
  return api;
}

it("kendi kartında: Pasifleştir ve kendi yönetici rolü pasif, nedenleri yazılı", async () => {
  await openCard({ userId: founderId, fullName: "Ayşe Kurucu", isSelf: true, roles: [{ role: "Founder", branchIds: [] }] });

  expect(screen.getByRole("button", { name: "Pasifleştir" })).toBeDisabled();
  expect(screen.getByRole("button", { name: "Pasifleştir" })).toHaveAccessibleDescription("Kendinizi pasifleştiremezsiniz");
  expect(screen.getByLabelText("Kurucu / Yönetici")).toBeDisabled();
  expect(screen.getByLabelText("Kurucu / Yönetici")).toHaveAccessibleDescription("Kendi yönetici rolünüzü alamazsınız");
});

it("son etkin yönetici başkasıysa da korunur", async () => {
  await openCard({ userId: founderId, fullName: "Tek Yönetici", roles: [{ role: "Founder", branchIds: [] }] }, [], 1);

  expect(screen.getByRole("button", { name: "Pasifleştir" })).toHaveAccessibleDescription("Okulda en az bir etkin yönetici kalmalı");
});

it("başka okulda da kayıtlı kişide Şifreyi sıfırla pasif ve açıklama yazılı", async () => {
  await openCard({ canResetPassword: false });

  expect(screen.getByRole("button", { name: "Şifreyi sıfırla" })).toBeDisabled();
  expect(screen.getByText("Bu kişi başka bir okulda da kayıtlı; şifresini kendisi 'Şifremi unuttum' ile yenilemeli.")).toBeInTheDocument();
});

it("şifre sıfırla: onay → geçici şifre bir kez gösterilir; Tamam ile karta dönülür", async () => {
  const api = await openCard({}, [["POST", /reset-password$/, () => json(200, { temporaryPassword: "Zx8cVb7nMq2w" })]]);

  await userEvent.click(screen.getByRole("button", { name: "Şifreyi sıfırla" }));
  expect(screen.getByRole("dialog")).toHaveTextContent("Şifre sıfırlansın mı?");
  await userEvent.click(screen.getByRole("button", { name: "Onayla" }));

  expect(await screen.findByText("Zx8cVb7nMq2w")).toBeInTheDocument();
  expect(api.sent("POST", /reset-password$/)).toHaveLength(1);
  await userEvent.click(screen.getByRole("button", { name: "Tamam" }));
  expect(await screen.findByRole("heading", { level: 3 })).toBeInTheDocument();
  expect(screen.queryByText("Zx8cVb7nMq2w")).not.toBeInTheDocument();
});

it("pasifleştir onaylı gider; pasif kişide Etkinleştir görünür", async () => {
  let active = true;
  const api = await openCard({}, [
    ["POST", /deactivate$/, () => {
      active = false;
      return json(200, memberDetail({ isActive: false }));
    }],
  ]);
  api.requests.length = 0;
  vi.stubGlobal("fetch", async (request: Request) => {
    const url = new URL(request.url, "http://x");
    if (request.method === "GET" && /^\/api\/admin\/members\/[^/]+$/.test(url.pathname)) {
      return json(200, memberDetail({ isActive: active }));
    }

    return api.fetch(request);
  });

  await userEvent.click(screen.getByRole("button", { name: "Pasifleştir" }));
  await userEvent.click(screen.getByRole("button", { name: "Onayla" }));

  expect(await screen.findByRole("button", { name: "Etkinleştir" })).toBeInTheDocument();
  expect(api.sent("POST", /deactivate$/)).toHaveLength(1);
});

it("profil: ad boşsa alan hatası ve istek yok; roller kaydedilir (şubesiz rol istek göndermez)", async () => {
  const api = await openCard({}, [
    ["PUT", /roles$/, () => json(200, memberDetail({ roles: [{ role: "Teacher", branchIds: [branchB.id] }] }))],
  ]);

  await userEvent.clear(screen.getByLabelText("Ad soyad"));
  await userEvent.click(screen.getByRole("button", { name: "Kaydet" }));
  expect(screen.getByText("Ad soyad zorunludur")).toBeInTheDocument();
  expect(api.sent("PUT", /profile$/)).toHaveLength(0);

  await userEvent.click(screen.getByLabelText("Veli"));
  await userEvent.click(screen.getByRole("button", { name: "Rolleri kaydet" }));
  expect(await screen.findByText("Bu rol için en az bir şube seçin")).toBeInTheDocument();
  expect(api.sent("PUT", /roles$/)).toHaveLength(0);

  await userEvent.click(screen.getByLabelText("Veli"));
  await userEvent.click(screen.getByRole("button", { name: "Rolleri kaydet" }));
  await waitFor(() => expect(api.sent("PUT", /roles$/)).toHaveLength(1));
  const body = (await api.sent("PUT", /roles$/)[0].json()) as Record<string, unknown>;
  expect(body).toEqual({ roles: [{ role: "Teacher", branchIds: [branchA.id] }] });
  void teacherId;
});

it("pasif şubedeki mevcut atama 'pasif' etiketiyle görünür ve kaldırılabilir (review M-1)", async () => {
  const inactive = { ...branchB, isActive: false };
  const api = adminFake([
    ["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA, inactive])],
    ["GET", /^\/api\/admin\/members$/, () => json(200, page([memberRow({ roles: [{ role: "Teacher", branchIds: [branchA.id, branchB.id] }] })], 2))],
    ["GET", /^\/api\/admin\/members\/[^/]+$/, () => json(200, memberDetail({ roles: [{ role: "Teacher", branchIds: [branchA.id, branchB.id] }] }))],
    ["PUT", /roles$/, () => json(200, memberDetail())],
  ]);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/yonetim";
  render(<App />);
  await userEvent.click(await screen.findByRole("tab", { name: "Kullanıcılar" }));
  await userEvent.click(await screen.findByRole("button", { name: /— Aç$/ }));

  const inactiveBox = await screen.findByLabelText(/Avcılar \(pasif\)/);
  expect(inactiveBox).toBeChecked();
  await userEvent.click(inactiveBox);
  await userEvent.click(screen.getByRole("button", { name: "Rolleri kaydet" }));

  await waitFor(() => expect(api.sent("PUT", /roles$/)).toHaveLength(1));
  expect(await api.sent("PUT", /roles$/)[0].json()).toEqual({ roles: [{ role: "Teacher", branchIds: [branchA.id] }] });
});

it("başka okulda da kayıtlı kişide ad/telefon salt okunur, Kaydet pasif ve nedeni yazılı (review M-2)", async () => {
  await openCard({ canEditProfile: false, canResetPassword: false });

  const reason = "Bu kişi başka bir okulda da kayıtlı; adını ve telefonunu kendisi değiştirebilir.";
  expect(screen.getByLabelText("Ad soyad")).toHaveAttribute("readonly");
  expect(screen.getByLabelText("Ad soyad")).toHaveAccessibleDescription(reason);
  expect(screen.getByLabelText("Telefon")).toHaveAttribute("readonly");
  expect(screen.getByRole("button", { name: "Kaydet" })).toBeDisabled();
});

it("rolü olmayan pasif kişide Etkinleştir pasif ve 'Önce rol verin' yazılı (review L-6)", async () => {
  await openCard({ isActive: false, roles: [] });

  const button = screen.getByRole("button", { name: "Etkinleştir" });
  expect(button).toBeDisabled();
  expect(button).toHaveAccessibleDescription("Önce rol verin: rolü olmayan kişi okulu kullanamaz");
});

it("onay iletişimi: Tab içinde döner, Vazgeç ile kapanınca odak açan düğmeye döner (review L-4)", async () => {
  await openCard({});
  const opener = screen.getByRole("button", { name: "Şifreyi sıfırla" });

  await userEvent.click(opener);
  expect(screen.getByRole("button", { name: "Vazgeç" })).toHaveFocus();
  await userEvent.tab();
  expect(screen.getByRole("button", { name: "Onayla" })).toHaveFocus();
  await userEvent.tab();
  expect(screen.getByRole("button", { name: "Vazgeç" })).toHaveFocus();
  await userEvent.tab({ shift: true });
  expect(screen.getByRole("button", { name: "Onayla" })).toHaveFocus();
  await userEvent.click(screen.getByRole("button", { name: "Vazgeç" }));

  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  expect(opener).toHaveFocus();
});
