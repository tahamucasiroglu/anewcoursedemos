import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { adminFake, branchA, classroom, json, levelFifth, termAutumn, type Route } from "./adminFake";

// Spec 0010 AC-1 (web, Sınıflar sekmesi → "Öğretmenler"): atanmış öğretmenler ve şubenin adayları; ekle/kaldır ve tek
// "Kaydet" (gövde kişi kimlikleri); kaydediliyor (düğme kilitli) → "Öğretmenler kaydedildi."; sunucu hatası duyurulur;
// şubede öğretmen yoksa "Bu şubede öğretmen yok"; başka satıra geçince editör o sınıfın verisiyle açılır (T15); iki tema.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

const ali = { userId: "t1", fullName: "Ali Öğretmen", email: "ali@ornek.test" };
const zeynep = { userId: "t2", fullName: "Zeynep Öğretmen", email: "zeynep@ornek.test" };
const classB = classroom({ id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaab2", name: "5-B" });

function view(assigned: unknown[], candidates: unknown[], name = "5-A") {
  return { classroomId: "c", classroomName: name, branchName: "Merkez", assigned, candidates };
}

async function openTab(routes: Route[]) {
  const api = adminFake([
    ...routes,
    ["GET", /^\/api\/admin\/terms$/, () => json(200, [termAutumn])],
    ["GET", /^\/api\/admin\/grade-levels$/, () => json(200, [levelFifth])],
    ["GET", /^\/api\/admin\/branches$/, () => json(200, [branchA])],
    ["GET", /^\/api\/admin\/classrooms$/, () => json(200, { termId: termAutumn.id, items: [classroom(), classB] })],
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

  it("Öğretmenler: Zeynep eklenir, Ali kaldırılır, Kaydet → kaydediliyor → kaydedildi; gövde kimlikler", async () => {
    let release: () => void = () => {};
    const held = new Promise<void>((resolve) => {
      release = resolve;
    });
    const api = await openTab([
      ["GET", /^\/api\/admin\/classrooms\/[^/]+\/teachers$/, () => json(200, view([ali], [zeynep]))],
      ["PUT", /^\/api\/admin\/classrooms\/[^/]+\/teachers$/, async () => {
        await held;
        return json(200, view([zeynep], [ali]));
      }],
    ]);

    await userEvent.click(await screen.findByRole("button", { name: "Merkez 5-A: Öğretmenler" }));
    const editor = await screen.findByRole("region", { name: "Merkez 5-A — öğretmenler" });
    await userEvent.click(within(editor).getByRole("button", { name: "Zeynep Öğretmen: Ekle" }));
    await userEvent.click(within(editor).getByRole("button", { name: "Ali Öğretmen: Kaldır" }));
    expect(within(editor).getByRole("button", { name: "Ali Öğretmen: Ekle" })).toBeInTheDocument();
    await userEvent.click(within(editor).getByRole("button", { name: "Kaydet" }));

    expect(await within(editor).findByRole("button", { name: "Kaydediliyor…" })).toBeDisabled();
    release();
    expect(await within(editor).findByText("Öğretmenler kaydedildi.")).toBeInTheDocument();
    const put = api.requests.find((r) => r.method === "PUT")!;
    expect(new URL(put.url, "http://x").pathname).toBe(`/api/admin/classrooms/${classroom().id}/teachers`);
    expect(await put.json()).toEqual({ teacherUserIds: ["t2"] });
  });
});

it("sunucu reddi duyurulur; başka satır kendi verisiyle açılır; şubede öğretmen yoksa mesaj", async () => {
  await openTab([
    ["GET", /^\/api\/admin\/classrooms\/aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1\/teachers$/, () => json(200, view([], [ali]))],
    ["GET", /^\/api\/admin\/classrooms\/aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaab2\/teachers$/, () => json(200, view([], [], "5-B"))],
    ["PUT", /^\/api\/admin\/classrooms\/[^/]+\/teachers$/, () => json(400, { detail: "Seçilen kişi bu sınıfın şubesinde etkin öğretmen değil" })],
  ]);

  await userEvent.click(await screen.findByRole("button", { name: "Merkez 5-A: Öğretmenler" }));
  const editor = await screen.findByRole("region", { name: "Merkez 5-A — öğretmenler" });
  expect(within(editor).getByText("Henüz öğretmen atanmadı")).toBeInTheDocument();
  await userEvent.click(within(editor).getByRole("button", { name: "Ali Öğretmen: Ekle" }));
  await userEvent.click(within(editor).getByRole("button", { name: "Kaydet" }));
  expect(await within(editor).findByRole("alert")).toHaveTextContent("Seçilen kişi bu sınıfın şubesinde etkin öğretmen değil");

  await userEvent.click(screen.getByRole("button", { name: "Merkez 5-B: Öğretmenler" }));
  const other = await screen.findByRole("region", { name: "Merkez 5-B — öğretmenler" });
  expect(within(other).getByText("Bu şubede öğretmen yok")).toBeInTheDocument();
  expect(within(other).queryByText("Ali Öğretmen")).toBeNull();
});
