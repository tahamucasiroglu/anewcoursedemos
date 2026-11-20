// Veli bildirimlerinin ortak gösterim mantığı (spec 0010): okunmamış sayılı bağlantı yazısı ve satırın etiketleri
// ("Yeni", "Düzeltildi"). Metinler ekranın tr.json'undan gelir.
import type { components } from "../schema";

export type NoticeView = components["schemas"]["NoticeView"];

/** "Bildirimler" ya da "Bildirimler (2)". */
export function noticesLinkText(unread: number | string | undefined, plain: string, withCount: string): string {
  const count = Number(unread ?? 0);
  return count > 0 ? withCount.replace("{n}", String(count)) : plain;
}

/** Satırın etiketleri: okunmamışsa "yeni", düzeltildiyse "düzeltildi". */
export function noticeBadges(notice: NoticeView): { isNew: boolean; corrected: boolean } {
  return { isNew: !notice.isRead, corrected: notice.kind === "Corrected" };
}
