// Ekran çağrılarının ortak sonucu (spec 0005/0006): başarı (veri), sunucu hatası (Türkçe mesaj) ya da sunucuya
// ulaşılamıyor. Ekranlar yalnızca bu üç durumu çizer.
export type ApiOutcome<T> =
  | { ok: true; data: T }
  | { ok: false; unreachable: true }
  | { ok: false; unreachable: false; status: number; message: string | null; fields?: ProblemField[] };

/** Doğrulama hatasındaki bir alan mesajı (ör. `Grades[<öğrenci>].Score` → "Puan 0 ile 100 arasında tam sayı olmalı"). */
export interface ProblemField {
  property: string;
  message: string;
}

export async function callApi<T>(request: Promise<{ data?: T; error?: unknown; response: Response }>): Promise<ApiOutcome<T>> {
  try {
    const { data, error, response } = await request;
    if (response.ok) {
      return { ok: true, data: data as T };
    }

    const fields = problemFields(error);
    return { ok: false, unreachable: false, status: response.status, message: problemMessage(error, fields), fields };
  } catch {
    return { ok: false, unreachable: true };
  }
}

/**
 * Kullanıcıya gösterilecek mesaj. Doğrulama hatasında alan mesajı (Türkçe) önce gelir: nArchitecture `detail`'e sabit İngilizce
 * "One or more validation errors occurred." yazar, Türkçe mesajlar `errors` listesindedir (T20; sunucu artık ilk alan mesajını
 * `detail`'e de kopyalar, istemci yine de alan listesini okur). Alan yoksa `detail`.
 */
export function problemMessage(error: unknown, fields: readonly ProblemField[] = problemFields(error)): string | null {
  if (fields.length > 0) {
    return fields[0].message;
  }

  const detail = pick(error, "detail");
  return typeof detail === "string" && detail !== "" ? detail : null;
}

/**
 * Hata gövdesindeki alan mesajları. İki biçim okunur: nArchitecture'ın dizisi (`Errors: [{ Property, Errors: [...] }]` —
 * büyük ya da küçük harfle) ve ASP.NET'in sözlüğü (`errors: { alan: [...] }`).
 */
export function problemFields(error: unknown): ProblemField[] {
  const errors = pick(error, "errors");
  const fields: ProblemField[] = [];
  if (Array.isArray(errors)) {
    for (const item of errors) {
      const property = pick(item, "property");
      for (const message of strings(pick(item, "errors"))) {
        fields.push({ property: typeof property === "string" ? property : "", message });
      }
    }
  } else if (errors && typeof errors === "object") {
    for (const [property, messages] of Object.entries(errors as Record<string, unknown>)) {
      for (const message of strings(messages)) {
        fields.push({ property, message });
      }
    }
  }

  return fields;
}

/** Nesnedeki alanı küçük harfle ya da baş harfi büyük yazılmış haliyle okur (`errors` / `Errors`). */
function pick(value: unknown, name: string): unknown {
  if (!value || typeof value !== "object") {
    return undefined;
  }

  const record = value as Record<string, unknown>;
  return name in record ? record[name] : record[name.charAt(0).toUpperCase() + name.slice(1)];
}

function strings(value: unknown): string[] {
  return Array.isArray(value) ? value.filter((m): m is string => typeof m === "string" && m !== "") : typeof value === "string" && value !== "" ? [value] : [];
}
