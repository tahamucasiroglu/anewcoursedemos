// Öğrenci ekranlarının ortak yardımcıları (spec 0009): yönetim çağrı sonucu (başarı / sunucu hatası / ulaşılamıyor) ve
// doğum tarihi kuralı için Türkiye günü ("yyyy-MM-dd"; sunucu da Europe/Istanbul gününe göre denetler).
export { call } from "../admin/adminApi";
export type { ApiOutcome } from "../admin/adminApi";

export function turkeyToday(now: Date = new Date()): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Istanbul", year: "numeric", month: "2-digit", day: "2-digit" }).format(now);
}
