import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { turkeyToday } from "@okul/api-client";
import App from "../../App";
import type { Route } from "../admin/adminFake";
import { announcementId, communicationFake, json } from "./communicationFake";

// Spec 0011 AC-12 ve AC-5 (web formu): kurucuda dört kapsam, öğretmende yalnızca sınıf/öğrenci; "Onay iste" soru ve son tarih
// alanlarını açar; alan hataları alanla ilişkili ve istek gitmez; gönderilirken düğme kilitli; sunucu hatası duyurulur;
// yayınlanınca ayrıntı açılır ve gövde formun değerlerini taşır.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

function start(extra: Route[] = [], roles = ["Founder"]) {
  const api = communicationFake(extra, roles);
  vi.stubGlobal("fetch", api.fetch);
  render(<App />);
  window.location.hash = "#/iletisim";
  return api;
}

function addDays(days: number): string {
  return new Date(Date.parse(`${turkeyToday()}T00:00:00Z`) + days * 86_400_000).toISOString().slice(0, 10);
}

async function openForm() {
  await userEvent.click(await screen.findByRole("button", { name: "Yeni duyuru" }));
  return screen.findByRole("form", { name: "Yeni duyuru" });
}

function scopeOptions() {
  return within(screen.getByLabelText("Kapsam")).getAllByRole("option").map((o) => o.textContent);
}

it("kurucu dört kapsamı, öğretmen yalnızca sınıf ve öğrenciyi seçebilir", async () => {
  start();
  await openForm();
  expect(scopeOptions()).toEqual(["Okulun tamamı", "Şube", "Sınıf", "Öğrenci"]);
});

it("öğretmen: kapsam seçenekleri yalnızca sınıf ve öğrenci; sınıflar kendi sınıflarıdır", async () => {
  start([], ["Teacher"]);
  await openForm();
  expect(scopeOptions()).toEqual(["Sınıf", "Öğrenci"]);
  expect(await within(screen.getByLabelText("Sınıf")).findByRole("option", { name: "5-A · Merkez" })).toBeInTheDocument();
});

it("alan hataları alanla ilişkili, istek gitmez; 'Onay iste' soru ve son tarih alanlarını açar", async () => {
  const api = start();
  await openForm();
  expect(screen.queryByLabelText("Onay sorusu")).not.toBeInTheDocument();
  await userEvent.click(screen.getByRole("checkbox", { name: "Onay iste" }));
  expect(screen.getByLabelText("Onay sorusu")).toBeInTheDocument();
  await userEvent.type(screen.getByLabelText("Son tarih"), turkeyToday());
  await userEvent.click(screen.getByRole("button", { name: "Yayınla" }));

  expect(screen.getByLabelText("Başlık")).toHaveAttribute("aria-invalid", "true");
  expect(screen.getByLabelText("Başlık")).toHaveAccessibleDescription("Başlık zorunludur");
  expect(screen.getByLabelText("Metin")).toHaveAccessibleDescription("Metin zorunludur");
  expect(screen.getByLabelText("Sınıf")).toHaveAccessibleDescription("Kapsam seçilmelidir");
  expect(screen.getByLabelText("Onay sorusu")).toHaveAccessibleDescription("Onay sorusu zorunludur");
  expect(screen.getByLabelText("Son tarih")).toHaveAccessibleDescription("Son tarih yarından en geç 180 gün sonrasına kadar olabilir");
  expect(api.requests.some((r) => r.method === "POST" && r.path === "/api/announcements")).toBe(false);
});

it("yayınlanırken düğme kilitli; sunucu hatası duyurulur, form korunur", async () => {
  let release: () => void = () => {};
  const held = new Promise<void>((resolve) => {
    release = resolve;
  });
  start([["POST", /^\/api\/announcements$/, async () => {
    await held;
    return json(400, { detail: "Bu kapsamda duyuru alacak veli yok." });
  }]]);
  await openForm();
  await userEvent.selectOptions(screen.getByLabelText("Kapsam"), "School");
  await userEvent.type(screen.getByLabelText("Başlık"), "Tatil");
  await userEvent.type(screen.getByLabelText("Metin"), "Okul kapalı.");
  await userEvent.click(screen.getByRole("button", { name: "Yayınla" }));

  const sending = await screen.findByRole("button", { name: "Yayınlanıyor…" });
  expect(sending).toBeDisabled();
  release();
  expect(await screen.findByText("Bu kapsamda duyuru alacak veli yok.")).toHaveAttribute("role", "alert");
  expect(screen.getByLabelText("Başlık")).toHaveValue("Tatil");
});

it("onay formu yayınlanır: gövde kapsam, soru ve son tarihi taşır; ayrıntı açılır", async () => {
  const api = start([["POST", /^\/api\/announcements$/, () => json(200, { id: announcementId, recipientCount: 22 })]]);
  await openForm();
  await userEvent.selectOptions(await screen.findByLabelText("Sınıf"), "k1");
  await userEvent.type(screen.getByLabelText("Başlık"), " Müze gezisi ");
  await userEvent.type(screen.getByLabelText("Metin"), "Pazartesi müze gezisi var.");
  await userEvent.click(screen.getByRole("checkbox", { name: "Onay iste" }));
  await userEvent.type(screen.getByLabelText("Onay sorusu"), "Geziye katılsın mı?");
  const deadline = addDays(7);
  await userEvent.type(screen.getByLabelText("Son tarih"), deadline);
  await userEvent.click(screen.getByRole("button", { name: "Yayınla" }));

  expect(await screen.findByRole("heading", { name: "Müze gezisi" })).toBeInTheDocument();
  expect(api.requests.find((r) => r.method === "POST" && r.path === "/api/announcements")!.body).toEqual({
    title: "Müze gezisi",
    body: "Pazartesi müze gezisi var.",
    scope: "Classroom",
    scopeId: "k1",
    requestConsent: true,
    consentQuestion: "Geziye katılsın mı?",
    consentDeadline: deadline,
  });
});

it("öğrenci kapsamı: önce sınıf, sonra o sınıfın öğrencisi (öğretmende bugünkü liste)", async () => {
  const api = start([["GET", /^\/api\/attendance\/classrooms\/k1\/today$/, () => json(200, {
    classroomId: "k1", classroomName: "5-A", branchName: "Merkez", day: "2026-11-20", isSchoolDay: true, isSaved: false, savedTime: null,
    students: [{ studentId: "s1", fullName: "Ali Yılmaz", status: "Present" }], version: 0,
  })], ["POST", /^\/api\/announcements$/, () => json(200, { id: announcementId, recipientCount: 2 })]], ["Teacher"]);
  await openForm();
  await userEvent.selectOptions(screen.getByLabelText("Kapsam"), "Student");
  await userEvent.selectOptions(await screen.findByLabelText("Sınıf"), "k1");
  await userEvent.selectOptions(screen.getByLabelText("Öğrenci"), await screen.findByRole("option", { name: "Ali Yılmaz" }));
  await userEvent.type(screen.getByLabelText("Başlık"), "Ali için");
  await userEvent.type(screen.getByLabelText("Metin"), "Not");
  await userEvent.click(screen.getByRole("button", { name: "Yayınla" }));

  expect(await screen.findByRole("heading", { name: "Müze gezisi" })).toBeInTheDocument();
  expect(api.requests.find((r) => r.method === "POST" && r.path === "/api/announcements")!.body).toMatchObject({ scope: "Student", scopeId: "s1" });
});
