// Ortak API istemcisi: web ve mobil backend'e yalnızca buradan ulaşır (docs/architecture.md, yasak bağımlılık 7).
import createClient from "openapi-fetch";
import type { paths } from "./schema";

export type OkulApiClient = ReturnType<typeof createOkulApiClient>;

/**
 * Verilen API adresine (ortam ayarından gelir) bağlı, tipleri openapi.json'dan üretilmiş istemci.
 * fetch her istekte çağrı anında bulunur (ortamın/testin fetch'i sonradan değişse de doğru olanı kullanılır).
 */
export function createOkulApiClient(baseUrl: string) {
  return createClient<paths>({ baseUrl, fetch: (request: Request) => globalThis.fetch(request) });
}
