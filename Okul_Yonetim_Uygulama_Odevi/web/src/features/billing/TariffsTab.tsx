import type { AuthSession } from "@okul/api-client";
import { CatalogTab } from "./CatalogTab";

/** Tarifeler sekmesi (spec 0015 gereksinim 1; yalnızca kurucu): ad + yıllık tutar; ortak gövde CatalogTab. */
export function TariffsTab({ client }: { client: AuthSession["client"] }) {
  return <CatalogTab client={client} kind="tariffs" />;
}
