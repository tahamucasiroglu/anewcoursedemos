// Veli bildirimlerinin ortak gösterim mantığı (spec 0010): okunmamış sayılı bağlantı yazısı ve satırın etiketleri
// ("Yeni", "Düzeltildi"). Metinler ekranın tr.json'undan gelir.
import type { components } from "../schema";

export type NoticeView = components["schemas"]["NoticeView"];

/** "Bildirimler" ya da "Bildirimler (2)". */
export function noticesLinkText(unread: number | string | undefined, plain: string, withCount: string): string {
  const count = Number(unread ?? 0);
  return count > 0 ? withCount.replace("{n}", String(count)) : plain;
}

/**
 * "Okundu" isteğinin gövdesi (0010 review F-3 ve review 2): listenin en yeni zamanı (liste yeniden eskiye sıralı; ilk satırın
 * `at` değeri olduğu gibi — tarih nesnesine çevrilmez, mikro saniye kaybolmasın). Sunucu bu zamana kadar değişen bütün
 * okunmamışları okundu yapar (listede gösterilmeyen eskiler de); liste ile istek arasında gelen ya da düzeltilen bildirim
 * okunmamış kalır. Liste boşsa istek gönderilmez (`null`).
 */
export function markReadBody(shown: readonly NoticeView[]): { upTo: string } | null {
  return shown.length === 0 ? null : { upTo: shown[0].at };
}

/** Satırın etiketleri: okunmamışsa "yeni", düzeltildiyse "düzeltildi". */
export function noticeBadges(notice: NoticeView): { isNew: boolean; corrected: boolean } {
  return { isNew: !notice.isRead, corrected: notice.kind === "Corrected" };
}
