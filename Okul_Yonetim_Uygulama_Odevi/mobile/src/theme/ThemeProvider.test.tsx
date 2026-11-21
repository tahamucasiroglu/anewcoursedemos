import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors, type ColorScheme } from "@okul/theme";
import { act, fireEvent, render, screen } from "@testing-library/react-native";
import { StyleSheet } from "react-native";
import App from "../../App";
import { warmUpFirstRender } from "../test/warmUp";
import tr from "../i18n/tr.json";

// Spec 0002: mobil tema — cihaz modu (useColorScheme) ve durum çubuğu taklit edilir; ağ katmanı taklit edilir.
let mockDevice: "light" | "dark" | null = null;
jest.mock("react-native/Libraries/Utilities/useColorScheme", () => ({ __esModule: true, default: () => mockDevice }));

const mockStatusBarStyles: string[] = [];
jest.mock("expo-status-bar", () => ({
  StatusBar: ({ style }: { style: string }) => {
    mockStatusBarStyles.push(style);
    return null;
  },
}));

const fetchMock = jest.fn<Promise<Response>, [Request]>();
const originalFetch = globalThis.fetch;

beforeEach(async () => {
  mockDevice = null;
  mockStatusBarStyles.length = 0;
  await AsyncStorage.clear();
  fetchMock.mockReset();
  fetchMock.mockReturnValue(new Promise(() => {}));
  globalThis.fetch = fetchMock as unknown as typeof fetch;
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

// İlk çizimin soğuk başlangıç bedeli testlerden önce, doğrulamasız bir turda ödenir (B-36, conventions T21; mobile/src/test/warmUp.ts).
warmUpFirstRender(async () => {
  globalThis.fetch = (() => new Promise(() => {})) as unknown as typeof fetch;
  await renderApp();
});

async function renderApp() {
  await render(<App />);
  return screen.findByTestId("screen");
}

function background(): string {
  return StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor as string;
}

const radio = (name: string) => screen.getByRole("radio", { name });
const checked = (name: string) => radio(name).props.accessibilityState?.checked === true;

describe.each<ColorScheme>(["light", "dark"])("%s modda renkler tokens.json'dan (AC-1)", (scheme) => {
  it("zemin, başlık metni ve kenar ortak temayla aynı", async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, scheme);
    await renderApp();
    expect(background()).toBe(themeColors[scheme].background);
    // Spec 0004: giriş kartı da başlık rolünde; burada uygulama başlığı adıyla seçilir.
    const title = screen.getByRole("header", { name: "Okul Yönetim Platformu" });
    expect(StyleSheet.flatten(title.props.style).color).toBe(themeColors[scheme].text);
    const card = screen.getByTestId("system-status");
    expect(StyleSheet.flatten(card.props.style).borderColor).toBe(themeColors[scheme].border);
  });
});

describe("ilk açılış (AC-2)", () => {
  it.each([
    ["dark", "dark"],
    ["light", "light"],
    [null, "light"],
  ] as const)("seçim yokken cihaz %s → %s, Sistem seçili", async (device, expected) => {
    mockDevice = device;
    await renderApp();
    expect(background()).toBe(themeColors[expected].background);
    expect(checked(tr["theme.system"])).toBe(true);
  });
});

it("Koyu seçilince anında koyu ve durum çubuğu açık yazılı, cihaz açık olsa bile (AC-4)", async () => {
  mockDevice = "light";
  await renderApp();
  expect(mockStatusBarStyles.at(-1)).toBe("dark");
  await fireEvent.press(radio(tr["theme.dark"]));
  expect(background()).toBe(themeColors.dark.background);
  expect(mockStatusBarStyles.at(-1)).toBe("light");
  expect(checked(tr["theme.dark"])).toBe(true);
});

describe("hatırlama (AC-5)", () => {
  it.each([
    [tr["theme.dark"], "dark", "dark"],
    [tr["theme.light"], "light", "light"],
    [tr["theme.system"], "system", "dark"],
  ] as const)("%s seçimi yeniden başlatmada geçerli", async (label, stored, expected) => {
    mockDevice = "dark";
    await renderApp();
    await fireEvent.press(radio(tr["theme.light"]));
    await fireEvent.press(radio(label));
    expect(await AsyncStorage.getItem(THEME_STORAGE_KEY)).toBe(stored);
    await screen.unmount();

    await renderApp();
    expect(checked(label)).toBe(true);
    expect(background()).toBe(themeColors[expected].background);
  });

  it("seçim sunucuya gönderilmez", async () => {
    await renderApp();
    await fireEvent.press(radio(tr["theme.dark"]));
    const urls = fetchMock.mock.calls.map(([request]) => request.url);
    expect(urls.every((url) => url.endsWith("/api/system-health"))).toBe(true);
  });
});

describe("cihaz modu değişimi (AC-6)", () => {
  it("Sistem seçiliyken cihaz değişirse görünüm hemen değişir", async () => {
    mockDevice = "light";
    await renderApp();
    mockDevice = "dark";
    await screen.rerender(<App />);
    expect(background()).toBe(themeColors.dark.background);
  });

  it("Açık seçiliyken cihaz değişikliği görünümü değiştirmez", async () => {
    mockDevice = "light";
    await renderApp();
    await fireEvent.press(radio(tr["theme.light"]));
    mockDevice = "dark";
    await screen.rerender(<App />);
    expect(background()).toBe(themeColors.light.background);
  });
});

describe("saklama hatası (AC-7)", () => {
  it("kayıt bozuksa Sistem ile açılır", async () => {
    mockDevice = "dark";
    await AsyncStorage.setItem(THEME_STORAGE_KEY, "{bozuk");
    await renderApp();
    expect(checked(tr["theme.system"])).toBe(true);
    expect(background()).toBe(themeColors.dark.background);
  });

  it("okuma/yazma hata verirse hata göstermeden Sistem ile açılır, seçici çalışır", async () => {
    mockDevice = "light";
    jest.spyOn(AsyncStorage, "getItem").mockRejectedValueOnce(new Error("depo kapalı"));
    jest.spyOn(AsyncStorage, "setItem").mockRejectedValueOnce(new Error("depo kapalı"));
    await renderApp();
    expect(checked(tr["theme.system"])).toBe(true);
    await fireEvent.press(radio(tr["theme.dark"]));
    expect(background()).toBe(themeColors.dark.background);
  });

  it("okuma 500 ms'de bitmezse Sistem ile açılır; geç gelen kayıt seçimi ezmez (review round 1, L-1)", async () => {
    jest.useFakeTimers();
    try {
      mockDevice = "dark";
      let resolveLate: (value: string | null) => void = () => {};
      jest.spyOn(AsyncStorage, "getItem").mockReturnValueOnce(
        new Promise<string | null>((resolve) => {
          resolveLate = resolve;
        }),
      );
      await render(<App />);
      await act(async () => {
        jest.advanceTimersByTime(499);
      });
      expect(screen.queryByTestId("screen")).toBeNull();

      await act(async () => {
        jest.advanceTimersByTime(1);
      });
      expect(checked(tr["theme.system"])).toBe(true);
      expect(background()).toBe(themeColors.dark.background);

      await act(async () => {
        resolveLate("light");
      });
      expect(checked(tr["theme.system"])).toBe(true);
      expect(background()).toBe(themeColors.dark.background);
    } finally {
      jest.useRealTimers();
    }
  });
});
