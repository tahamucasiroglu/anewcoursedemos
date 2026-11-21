import type { AuthSession } from "@okul/api-client";
import { CatalogTab } from "./CatalogTab";

/** İndirimler sekmesi (spec 0015 gereksinim 2; yalnızca kurucu): ad + yüzde (1–100); ortak gövde CatalogTab. */
export function DiscountsTab({ client }: { client: AuthSession["client"] }) {
  return <CatalogTab client={client} kind="discounts" />;
}
