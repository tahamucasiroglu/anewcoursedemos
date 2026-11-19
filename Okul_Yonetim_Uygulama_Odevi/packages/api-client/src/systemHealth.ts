// Sistem durumu sorgusu (spec 0001): API'nin cevabını ekranların kullandığı üç sonuca çevirir.
import type { OkulApiClient } from "./client";

export type SystemHealthResult =
  | { kind: "healthy"; message: string }
  | { kind: "unavailable"; message: string }
  | { kind: "unreachable" };

/**
 * 200 → "healthy", 503 → "unavailable" (mesaj API'den, Türkçe); API'ye hiç ulaşılamazsa veya beklenmeyen bir
 * cevap gelirse → "unreachable" (ekran kendi Türkçe metnini gösterir).
 */
export async function fetchSystemHealth(client: OkulApiClient): Promise<SystemHealthResult> {
  try {
    const { data, error, response } = await client.GET("/api/system-health");
    if (response.status === 200 && data) {
      return { kind: "healthy", message: data.message };
    }
    if (response.status === 503 && error) {
      return { kind: "unavailable", message: error.message };
    }
    return { kind: "unreachable" };
  } catch {
    return { kind: "unreachable" };
  }
}
