import { DEFAULT_API_BASE_URL, resolveApiBaseUrl } from "./config";

describe("resolveApiBaseUrl", () => {
  it("ortam ayarındaki adresi kullanır", () => {
    expect(resolveApiBaseUrl("http://192.168.1.50:5080")).toBe("http://192.168.1.50:5080");
  });

  it('"/" verilirse sayfayla aynı adres kullanılır (spec 0003, Raspberry Pi; IP değişince yeniden derleme gerekmez)', () => {
    expect(resolveApiBaseUrl("/")).toBe("");
    expect(resolveApiBaseUrl(" / ")).toBe("");
  });

  it("ayar yoksa yerel API adresine düşer", () => {
    expect(resolveApiBaseUrl(undefined)).toBe(DEFAULT_API_BASE_URL);
    expect(resolveApiBaseUrl("  ")).toBe(DEFAULT_API_BASE_URL);
  });
});
