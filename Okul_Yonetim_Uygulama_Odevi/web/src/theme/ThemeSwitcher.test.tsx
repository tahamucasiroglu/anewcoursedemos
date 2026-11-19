import { THEME_STORAGE_KEY } from "@okul/theme";
import { act, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../App";
import tr from "../i18n/tr.json";

// Spec 0002: tema seçici uygulamanın içinde (başlık satırı) sınanır; ağ katmanı taklit edilir.
const fetchMock = vi.fn<(input: Request) => Promise<Response>>();

/** Cihazın açık/koyu ayarını taklit eder; change() ile "cihaz modu değişti" olayı gönderilir. */
function fakeDevice(initial: "dark" | "light" | null) {
  let dark = initial === "dark";
  const listeners = new Set<(event: MediaQueryListEvent) => void>();
  vi.stubGlobal(
    "matchMedia",
    initial === null
      ? undefined
      : () => ({
          get matches() {
            return dark;
          },
          addEventListener: (_: string, fn: (event: MediaQueryListEvent) => void) => listeners.add(fn),
          removeEventListener: (_: string, fn: (event: MediaQueryListEvent) => void) => listeners.delete(fn),
        }),
  );
  return {
    change(next: "dark" | "light") {
      dark = next === "dark";
      act(() => listeners.forEach((fn) => fn({ matches: dark } as MediaQueryListEvent)));
    },
  };
}

const theme = () => document.documentElement.getAttribute("data-theme");
const group = () => screen.getByRole("group", { name: tr["theme.label"] });
const option = (name: string) => within(group()).getByRole("button", { name });

beforeEach(() => {
  window.localStorage.clear();
  document.documentElement.removeAttribute("data-theme");
  fetchMock.mockReset();
  fetchMock.mockReturnValue(new Promise(() => {}));
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => {
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

describe("ilk açılış (AC-2)", () => {
  it.each([
    ["dark", "dark"],
    ["light", "light"],
    [null, "light"],
  ] as const)("seçim yokken cihaz %s → %s, Sistem seçili", (device, expected) => {
    fakeDevice(device);
    render(<App />);
    expect(theme()).toBe(expected);
    expect(option(tr["theme.system"])).toHaveAttribute("aria-pressed", "true");
  });
});

it("Tema grubunda Sistem, Açık, Koyu yazılı; seçili olan belirtilir (AC-3)", () => {
  fakeDevice("light");
  render(<App />);
  const buttons = within(group()).getAllByRole("button");
  expect(buttons.map((b) => b.textContent?.trim())).toEqual([
    `◐ ${tr["theme.system"]}`,
    `☀ ${tr["theme.light"]}`,
    `☾ ${tr["theme.dark"]}`,
  ]);
  expect(buttons.map((b) => b.getAttribute("aria-pressed"))).toEqual(["true", "false", "false"]);
  expect(group().closest("header")).toContainElement(screen.getByRole("heading", { name: tr["app.title"] }));
});

it("Koyu seçilince anında koyu, cihaz açık olsa bile; tarayıcı parçaları da koyu (AC-4)", async () => {
  fakeDevice("light");
  render(<App />);
  await userEvent.click(option(tr["theme.dark"]));
  expect(theme()).toBe("dark");
  expect(document.documentElement.style.colorScheme).toBe("dark");
  expect(option(tr["theme.dark"])).toHaveAttribute("aria-pressed", "true");
  expect(option(tr["theme.system"])).toHaveAttribute("aria-pressed", "false");
});

describe("hatırlama (AC-5)", () => {
  it.each([
    [tr["theme.dark"], "dark", "dark"],
    [tr["theme.light"], "light", "light"],
    [tr["theme.system"], "system", "dark"],
  ] as const)("%s seçimi yeniden açılışta geçerli", async (label, storedValue, expected) => {
    fakeDevice("dark");
    const first = render(<App />);
    await userEvent.click(option(tr["theme.light"]));
    await userEvent.click(option(label));
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe(storedValue);
    first.unmount();
    document.documentElement.removeAttribute("data-theme");

    render(<App />);
    expect(option(label)).toHaveAttribute("aria-pressed", "true");
    expect(theme()).toBe(expected);
  });

  it("seçim sunucuya gönderilmez", async () => {
    fakeDevice("light");
    render(<App />);
    await userEvent.click(option(tr["theme.dark"]));
    const urls = fetchMock.mock.calls.map(([request]) => request.url);
    expect(urls.every((url) => url.endsWith("/api/system-health"))).toBe(true);
    expect(urls.join(" ")).not.toMatch(/theme|dark/);
  });
});

describe("cihaz modu değişimi (AC-6)", () => {
  it("Sistem seçiliyken cihaz değişirse görünüm hemen değişir", () => {
    const device = fakeDevice("light");
    render(<App />);
    device.change("dark");
    expect(theme()).toBe("dark");
    device.change("light");
    expect(theme()).toBe("light");
  });

  it("Açık seçiliyken cihaz değişikliği görünümü değiştirmez", async () => {
    const device = fakeDevice("light");
    render(<App />);
    await userEvent.click(option(tr["theme.light"]));
    device.change("dark");
    expect(theme()).toBe("light");
  });
});

describe("saklama hatası (AC-7)", () => {
  it("kayıt bozuksa Sistem ile açılır", () => {
    fakeDevice("dark");
    window.localStorage.setItem(THEME_STORAGE_KEY, "{bozuk");
    render(<App />);
    expect(option(tr["theme.system"])).toHaveAttribute("aria-pressed", "true");
    expect(theme()).toBe("dark");
  });

  it("saklama okunamaz/yazılamazsa hata vermez, seçici çalışır", async () => {
    fakeDevice("light");
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
      throw new DOMException("Saklama kapalı", "SecurityError");
    });
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
      throw new DOMException("Saklama kapalı", "SecurityError");
    });
    render(<App />);
    expect(option(tr["theme.system"])).toHaveAttribute("aria-pressed", "true");
    await userEvent.click(option(tr["theme.dark"]));
    expect(theme()).toBe("dark");
  });
});

it("yalnızca klavyeyle: Tab ile odak, Enter ve Boşluk ile seçim (AC-11)", async () => {
  fakeDevice("light");
  render(<App />);
  const user = userEvent.setup();
  await user.tab();
  expect(option(tr["theme.system"])).toHaveFocus();
  await user.tab();
  await user.tab();
  expect(option(tr["theme.dark"])).toHaveFocus();
  await user.keyboard("{Enter}");
  expect(theme()).toBe("dark");
  await user.tab({ shift: true });
  expect(option(tr["theme.light"])).toHaveFocus();
  await user.keyboard(" ");
  expect(theme()).toBe("light");
  expect(option(tr["theme.light"])).toHaveAttribute("aria-pressed", "true");
});
