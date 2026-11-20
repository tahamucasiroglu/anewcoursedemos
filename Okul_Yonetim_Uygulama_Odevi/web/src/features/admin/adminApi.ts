// Yönetim çağrılarının ortak sonucu: başarı (veri), sunucu hatası (Türkçe mesaj, ör. "Bu adla bir şube zaten var") ya da
// sunucuya ulaşılamıyor. Ekranlar yalnızca bu üç durumu çizer.
export type ApiOutcome<T> = { ok: true; data: T } | { ok: false; unreachable: true } | { ok: false; unreachable: false; status: number; message: string | null };

export async function call<T>(request: Promise<{ data?: T; error?: unknown; response: Response }>): Promise<ApiOutcome<T>> {
  try {
    const { data, error, response } = await request;
    if (response.ok) {
      return { ok: true, data: data as T };
    }

    return { ok: false, unreachable: false, status: response.status, message: problemMessage(error) };
  } catch {
    return { ok: false, unreachable: true };
  }
}

function problemMessage(error: unknown): string | null {
  if (error && typeof error === "object") {
    if ("detail" in error && typeof error.detail === "string" && error.detail !== "") {
      return error.detail;
    }

    if ("errors" in error && error.errors && typeof error.errors === "object") {
      const first = Object.values(error.errors as Record<string, unknown>).flat()[0];
      if (typeof first === "string") {
        return first;
      }
    }
  }

  return null;
}
