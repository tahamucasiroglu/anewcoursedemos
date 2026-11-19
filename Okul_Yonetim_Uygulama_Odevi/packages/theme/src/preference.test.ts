import { parsePreference, resolveScheme } from "./preference";

// Spec 0002, AC-2 ve AC-7.
describe("parsePreference", () => {
  it.each(["system", "light", "dark"])("geçerli değer %s aynen döner", (value) => {
    expect(parsePreference(value)).toBe(value);
  });

  it.each([null, undefined, "", "DARK", "koyu", "{bozuk", 42])("bozuk/bilinmeyen değer %s → system", (value) => {
    expect(parsePreference(value)).toBe("system");
  });
});

describe("resolveScheme", () => {
  it("Sistem: cihaz koyuysa koyu, açıksa açık, bilgi yoksa açık", () => {
    expect(resolveScheme("system", "dark")).toBe("dark");
    expect(resolveScheme("system", "light")).toBe("light");
    expect(resolveScheme("system", null)).toBe("light");
    expect(resolveScheme("system", undefined)).toBe("light");
  });

  it("Açık/Koyu seçimi cihaz modundan bağımsızdır", () => {
    expect(resolveScheme("dark", "light")).toBe("dark");
    expect(resolveScheme("light", "dark")).toBe("light");
    expect(resolveScheme("dark", null)).toBe("dark");
  });
});
