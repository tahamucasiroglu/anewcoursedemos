import { fireEvent, render, screen, within } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";
import { warmUpFirstRender } from "../../test/warmUp";

// Spec 0010 AC-2 ve AC-13 (mobil, Sınıflarım): birden çok sınıfta liste (şube ve bugünkü durum, ekran okuyucu etiketi);
// satıra dokununca yoklama açılır, "Sınıflarıma dön" listeyi yeniler; yükleniyor → hata + tekrar dene → "Size atanmış sınıf
// yok" — iki temada.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const teacherMe = { fullName: "Ali Öğretmen", email: "ali@ornek.test", tenant: tenantA, roles: ["Teacher"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0 };
const classA = { id: "k1", name: "5-A", branchName: "Merkez", takenToday: true };
const classB = { id: "k2", name: "6-A", branchName: "Avcılar", takenToday: false };
const sheetB = { classroomId: "k2", classroomName: "6-A", branchName: "Avcılar", day: "2026-11-20", isSchoolDay: true, isSaved: false, savedTime: null, students: [{ studentId: "s1", fullName: "Ayşe Yılmaz", status: "Present" }] };

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

function fakeApi(classrooms: Handler, today?: Handler) {
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
      return json(200, teacherMe);
    }

    if (path === "/api/attendance/classrooms") {
      return classrooms(request);
    }

    if (path.endsWith("/today") && today) {
      return today(request);
    }

    return json(404, {});
  }) as typeof fetch;
  return requests;
}

async function openClassrooms() {
  await render(<App />);
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "ali@ornek.test");
  await fireEvent.changeText(screen.getByLabelText("Şifre"), "Sifre-1234");
  await fireEvent.press(screen.getByText("Giriş yap"));
  await fireEvent.press(await screen.findByRole("button", { name: "Yoklama al" }));
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
  fakeApi(() => json(200, [classA, classB]), () => json(200, sheetB));
  await openClassrooms();
  await screen.findAllByTestId("classroom-row");
});

describe.each(["light", "dark"] as const)("Sınıflarım — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("birden çok sınıf: liste durumla; satır yoklamayı açar; 'Sınıflarıma dön' listeyi yeniler", async () => {
    const requests = fakeApi(() => json(200, [classA, classB]), () => json(200, sheetB));
    await openClassrooms();

    expect(await screen.findByRole("header", { name: "Sınıflarım" })).toBeTruthy();
    expect(screen.getAllByTestId("classroom-row").map((r) => r.props.accessibilityLabel)).toEqual([
      "5-A, Merkez, Bugün alındı, yoklamayı aç",
      "6-A, Avcılar, Bugün alınmadı, yoklamayı aç",
    ]);
    const list = screen.getByTestId("classrooms-list");
    expect(list.type).toBe("RCTScrollView"); // 0010 review F-2: liste kayar
    expect(within(list).getAllByTestId("classroom-row")).toHaveLength(2);
    await fireEvent.press(screen.getByRole("button", { name: "6-A, Avcılar, Bugün alınmadı, yoklamayı aç" }));
    expect(await screen.findByRole("header", { name: "6-A" })).toBeTruthy();
    expect(screen.getByRole("radio", { name: "Ayşe Yılmaz: Var" })).toBeTruthy();

    await fireEvent.press(screen.getByRole("button", { name: "Sınıflarıma dön" }));
    expect(await screen.findByRole("header", { name: "Sınıflarım" })).toBeTruthy();
    expect(requests.filter((r) => r === "GET /api/attendance/classrooms")).toHaveLength(2);
  });
});

it("yükleniyor → hata + tekrar dene → 'Size atanmış sınıf yok'", async () => {
  let calls = 0;
  let fail: () => void = () => undefined;
  const failed = new Promise<Response>((resolve) => { fail = () => resolve(json(500, {})); });
  fakeApi(() => (++calls === 1 ? failed : json(200, [])));
  await openClassrooms();

  expect(await screen.findByText("Yükleniyor…")).toBeTruthy();
  fail();
  expect(await screen.findByRole("alert")).toHaveTextContent("Bilgiler alınamadı.");
  await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByText("Size atanmış sınıf yok")).toBeTruthy();
  await fireEvent.press(screen.getByRole("button", { name: "Hoş geldin'e dön" }));
  expect(await screen.findByTestId("welcome")).toBeTruthy();
});
