// Ortam ayarları: API adresi .env'den (VITE_API_BASE_URL); yoksa yerel API (ADR-0004).
// "/" verilirse web, sayfayla aynı adresteki API'yi kullanır (Raspberry Pi'de nginx /api'yi API'ye iletir; spec 0003).
export const DEFAULT_API_BASE_URL = "http://localhost:5080";

export const SAME_ORIGIN = "/";

export function resolveApiBaseUrl(value: string | undefined): string {
  const trimmed = value?.trim() ?? "";
  if (trimmed === SAME_ORIGIN) {
    return "";
  }

  return trimmed !== "" ? trimmed : DEFAULT_API_BASE_URL;
}

export const apiBaseUrl = resolveApiBaseUrl(import.meta.env.VITE_API_BASE_URL as string | undefined);
