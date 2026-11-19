import { parsePreference, resolveScheme, THEME_STORAGE_KEY, type ColorScheme } from "@okul/theme";
import indexHtml from "../../index.html?raw";

// Spec 0002, AC-7 ve AC-8: index.html'deki ön betik, sayfa ilk çizilmeden (ana betikten önce, <head>'de) kayıtlı
// seçimi uygular. Betik @okul/theme mantığının düz JS kopyası olduğu için aynı vakalarla sınanır (Known traps T11).
const html = new DOMParser().parseFromString(indexHtml, "text/html");
const prePaint = html.getElementById("theme-pre-paint");

function runPrePaint(stored: string | null | "throws", device: ColorScheme | null): string | null {
  document.documentElement.removeAttribute("data-theme");
  document.documentElement.style.colorScheme = "";
  vi.spyOn(Storage.prototype, "getItem").mockImplementation((key) => {
    if (stored === "throws") {
      throw new DOMException("Saklama kapalı", "SecurityError");
    }

    return key === THEME_STORAGE_KEY ? stored : null;
  });
  vi.stubGlobal(
    "matchMedia",
    device === null ? undefined : (query: string) => ({ matches: device === "dark" && query.includes("dark") }),
  );
  new Function(prePaint!.textContent!)();
  return document.documentElement.getAttribute("data-theme");
}

afterEach(() => {
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

it("ön betik <head>'de ve ana uygulama betiğinden önce", () => {
  expect(prePaint?.parentElement?.tagName).toBe("HEAD");
  const scripts = [...html.querySelectorAll("script")];
  const appScript = scripts.find((s) => s.getAttribute("type") === "module");
  expect(scripts.indexOf(prePaint as HTMLScriptElement)).toBeLessThan(scripts.indexOf(appScript!));
});

const stored = ["dark", "light", "system", null, "", "koyu", "{bozuk"] as const;
const devices: Array<ColorScheme | null> = ["dark", "light", null];

describe.each(stored)("kayıtlı değer %s", (value) => {
  it.each(devices)("cihaz %s → @okul/theme ile aynı sonuç", (device) => {
    const expected = resolveScheme(parsePreference(value), device);
    expect(runPrePaint(value, device)).toBe(expected);
    expect(document.documentElement.style.colorScheme).toBe(expected);
  });
});

it("kayıtlı Koyu: ilk görünüm koyu (cihaz açık olsa bile)", () => {
  expect(runPrePaint("dark", "light")).toBe("dark");
});

it("saklama hata verirse hata fırlatmadan Sistem'e göre açılır", () => {
  expect(runPrePaint("throws", "dark")).toBe("dark");
  expect(runPrePaint("throws", "light")).toBe("light");
});
