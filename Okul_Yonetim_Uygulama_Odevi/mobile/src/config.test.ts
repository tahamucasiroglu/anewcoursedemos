import { DEFAULT_API_BASE_URL, resolveApiBaseUrl } from "./config";

describe("resolveApiBaseUrl", () => {
  it("ortam ayarındaki adresi kullanır", () => {
    expect(resolveApiBaseUrl("http://192.168.1.50:5080")).toBe("http://192.168.1.50:5080");
  });

  it("ayar yoksa yerel API adresine düşer", () => {
    expect(resolveApiBaseUrl(undefined)).toBe(DEFAULT_API_BASE_URL);
    expect(resolveApiBaseUrl("  ")).toBe(DEFAULT_API_BASE_URL);
  });
});
