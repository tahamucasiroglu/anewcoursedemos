// Ortam ayarları: API adresi EXPO_PUBLIC_API_BASE_URL'den; yoksa yerel API (ADR-0004).
export const DEFAULT_API_BASE_URL = "http://localhost:5080";

export function resolveApiBaseUrl(value: string | undefined): string {
  return value && value.trim() !== "" ? value.trim() : DEFAULT_API_BASE_URL;
}

export const apiBaseUrl = resolveApiBaseUrl(process.env.EXPO_PUBLIC_API_BASE_URL);
