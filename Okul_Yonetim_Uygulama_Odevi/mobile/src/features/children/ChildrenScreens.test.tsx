import { fireEvent, render, screen, within } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";
import { warmUpFirstRender } from "../../test/warmUp";

// Spec 0009 AC-13 (mobil) ve AC-1/AC-9 ekran tarafı: "Çocuklarım" yalnızca veli rolünde; liste yükleniyor / boş / dolu /
// hata + tekrar dene (farklı şubelerdeki iki çocuk tek listede); salt okunur kart (sınıf atanmamış, teslim kişisi yok,
// sağlık gösterilemiyor; sağlık notu ve alerji ayrı başlık); bağ kalkınca "Bu öğrenci artık görüntülenemiyor." ve listeye
// dönüş; ekran okuyucu etiketleri — iki temada.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const guardianMe = { fullName: "Ayla Ak", email: "ayla@ornek.test", tenant: tenantA, roles: ["Guardian"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA] };
const zeynep = { id: "c1", fullName: "Zeynep Ak", branchName: "Merkez", classroomName: "3-A" };
const can = { id: "c2", fullName: "Can Ak", branchName: "Avcılar", classroomName: null };
const fullCard = {
  id: "c1",
  fullName: "Zeynep Ak",
  birthDate: "2018-05-04",
  schoolNumber: "301",
  branchName: "Merkez",
  classroomName: "3-A",
  health: { available: true, healthNote: "Astım", allergy: "Fıstık" },
  pickupPersons: [{ fullName: "Hasan Ak", relationship: "Dede", phone: "+905320001122" }],
};

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });
}

function fakeApi(handlers: { list?: Handler; card?: Handler; roles?: string[] }) {
  const requests: string[] = [];
  globalThis.fetch = (async (request: Request) => {
    const path = new URL(request.url).pathname;
    requests.push(`${request.method} ${path}`);
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/login") {
      return json(200, tokens);
    }

    if (path === "/api/me") {
      return json(200, { ...guardianMe, roles: handlers.roles ?? guardianMe.roles });
    }

    if (path === "/api/my/children" && handlers.list) {
      return handlers.list(request);
    }

    if (path.startsWith("/api/my/children/") && handlers.card) {
      return handlers.card(request);
    }

    return json(404, {});
  }) as typeof fetch;
  return requests;
}

async function signIn() {
  await render(<App />);
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "ayla@ornek.test");
  await fireEvent.changeText(screen.getByLabelText("Şifre"), "Sifre-1234");
  await fireEvent.press(screen.getByText("Giriş yap"));
  await screen.findByTestId("welcome");
}

async function openChildren() {
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Çocuklarım" }));
}

beforeEach(async () => {
  reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

// İlk çizimin soğuk başlangıç bedeli testlerden önce, doğrulamasız bir turda ödenir (B-36, conventions T21; mobile/src/test/warmUp.ts).
warmUpFirstRender(async () => {
  fakeApi({ list: () => json(200, [zeynep, can]) });
  await openChildren();
  await screen.findAllByTestId("child-row");
});

describe.each(["light", "dark"] as const)("Çocuklarım — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("dolu liste: farklı şubelerdeki iki çocuk, sınıfsız çocukta 'Sınıf atanmamış'; satır etiketi ekran okuyucu için", async () => {
    fakeApi({ list: () => json(200, [zeynep, can]) });
    await openChildren();

    expect(await screen.findByRole("header", { name: "Çocuklarım" })).toBeTruthy();
    const rows = await screen.findAllByTestId("child-row");
    const list = screen.getByTestId("children-list");
    expect(list.type).toBe("RCTScrollView"); // 0010 review F-2: liste kayar, "Geri" dışında
    expect(within(list).getAllByTestId("child-row")).toHaveLength(rows.length);
    expect(rows.map((r) => r.props.accessibilityLabel)).toEqual([
      "Zeynep Ak, Merkez, 3-A, kartını aç",
      "Can Ak, Avcılar, Sınıf atanmamış, kartını aç",
    ]);
  });

  it("yükleniyor → hata + tekrar dene → boş liste mesajı", async () => {
    let calls = 0;
    let fail: () => void = () => undefined;
    const failed = new Promise<Response>((resolve) => { fail = () => resolve(json(500, {})); });
    fakeApi({ list: () => (++calls === 1 ? failed : json(200, [])) });
    await openChildren();

    expect(await screen.findByText("Yükleniyor…")).toBeTruthy();
    fail();
    expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
    await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));

    expect(await screen.findByText("Henüz bağlı öğrenci yok")).toBeTruthy();
  });

  it("dolu kart: bilgiler, sağlık notu ve alerji ayrı başlıkla, teslim kişileri; Listeye dön", async () => {
    fakeApi({ list: () => json(200, [zeynep]), card: () => json(200, fullCard) });
    await openChildren();
    await fireEvent.press(await screen.findByRole("button", { name: "Zeynep Ak, Merkez, 3-A, kartını aç" }));

    expect(await screen.findByRole("header", { name: "Zeynep Ak" })).toBeTruthy();
    const scroll = screen.getByTestId("child-card-scroll");
    expect(scroll.type).toBe("RCTScrollView"); // 0010 review F-2: kart içeriği kayar, "Listeye dön" dışında
    expect(within(scroll).getByLabelText("Doğum tarihi: 04.05.2018")).toBeTruthy();
    expect(within(scroll).getAllByTestId("pickup-person").length).toBeGreaterThan(0);
    expect(screen.getByLabelText("Doğum tarihi: 04.05.2018")).toBeTruthy();
    expect(screen.getByLabelText("Sınıf: 3-A")).toBeTruthy();
    expect(screen.getByRole("header", { name: "Sağlık notu" })).toBeTruthy();
    expect(screen.getByText("Astım")).toBeTruthy();
    expect(screen.getByRole("header", { name: "Alerji" })).toBeTruthy();
    expect(screen.getByText("Fıstık")).toBeTruthy();
    expect(screen.getByLabelText("Hasan Ak, Dede, +905320001122")).toBeTruthy();
    expect(screen.queryByText(/ayla@ornek/)).toBeNull();

    await fireEvent.press(screen.getByRole("button", { name: "Listeye dön" }));
    expect(await screen.findByRole("header", { name: "Çocuklarım" })).toBeTruthy();
  });

  it("boş kart durumları: sınıf atanmamış, teslim kişisi yok, sağlık gösterilemiyor", async () => {
    fakeApi({
      list: () => json(200, [can]),
      card: () => json(200, { ...fullCard, id: "c2", fullName: "Can Ak", classroomName: null, schoolNumber: null, pickupPersons: [], health: { available: false, healthNote: null, allergy: null } }),
    });
    await openChildren();
    await fireEvent.press(await screen.findByRole("button", { name: "Can Ak, Avcılar, Sınıf atanmamış, kartını aç" }));

    expect(await screen.findByLabelText("Sınıf: Sınıf atanmamış")).toBeTruthy();
    expect(screen.getByText("Sağlık bilgisi şu anda gösterilemiyor.")).toBeTruthy();
    expect(screen.getByText("Henüz kimse eklenmedi")).toBeTruthy();
    expect(screen.queryByText("Astım")).toBeNull();
  });

  it("bağ kalktıysa: 'Bu öğrenci artık görüntülenemiyor.' ve listeye dönünce liste yenilenir", async () => {
    let listCalls = 0;
    const requests = fakeApi({ list: () => json(200, ++listCalls === 1 ? [zeynep] : []), card: () => json(404, { detail: "Bu öğrenci artık görüntülenemiyor." }) });
    await openChildren();
    await fireEvent.press(await screen.findByRole("button", { name: "Zeynep Ak, Merkez, 3-A, kartını aç" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Bu öğrenci artık görüntülenemiyor.");
    await fireEvent.press(screen.getByRole("button", { name: "Listeye dön" }));

    expect(await screen.findByText("Henüz bağlı öğrenci yok")).toBeTruthy();
    expect(requests.filter((r) => r === "GET /api/my/children")).toHaveLength(2);
  });
});

it("veli rolü olmayan kişide Hoş geldin'de 'Çocuklarım' yok", async () => {
  const requests = fakeApi({ roles: ["Teacher"] });
  await signIn();

  expect(screen.queryByRole("button", { name: "Çocuklarım" })).toBeNull();
  expect(screen.getByRole("button", { name: "Okul takvimi" })).toBeTruthy();
  expect(requests.some((r) => r.includes("/api/my/children"))).toBe(false);
});
