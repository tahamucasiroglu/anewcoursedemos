// Para metni (spec 0015, Accessibility: "1.234,50 TL"). Sunucu tutarı JSON sayısı olarak (en fazla iki ondalık) gönderir; OpenAPI tipi
// "sayı ya da metin" der. Tutar önce tam kuruşa çevrilir — sayıysa Math.round(değer × 100) (iki ondalıklı değerde kesin), metinse
// rakamlarından (kayan nokta hiç yok) — ve metin kuruş tamsayısından kurulur; tarayıcının yerel ayarına (Intl) bağlı değildir.
export type Amount = number | string;

/** Tutarın kuruş tamsayısı. */
export function toCents(amount: Amount): number {
  if (typeof amount === "number") {
    return Math.round(amount * 100);
  }

  const match = /^\s*(-?)(\d+)(?:\.(\d{0,2})\d*)?\s*$/.exec(amount);
  if (!match) {
    return 0;
  }

  const cents = Number(match[2]) * 100 + Number((match[3] ?? "").padEnd(2, "0"));
  return match[1] === "-" ? -cents : cents;
}

/** Kuruş tamsayısından "1.234,50" (binlik nokta, ondalık virgül, her zaman iki ondalık; eksi işaretli olabilir). */
export function centsText(cents: number): string {
  const negative = cents < 0;
  const abs = Math.abs(Math.trunc(cents));
  const lira = Math.floor(abs / 100).toString();
  const kurus = (abs % 100).toString().padStart(2, "0");
  const grouped = lira.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
  return `${negative ? "-" : ""}${grouped},${kurus}`;
}

/** Tutardan "1.234,50". */
export function moneyText(amount: Amount): string {
  return centsText(toCents(amount));
}

/** Tutardan "1.234,50 TL". */
export function moneyTl(amount: Amount): string {
  return `${moneyText(amount)} TL`;
}

/** "2026-12-01" → "01.12.2026" (gün metni; saat dilimi dönüşümü yok — sunucunun Türkiye günü olduğu gibi). */
export function dayText(isoDay: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(isoDay);
  return match ? `${match[3]}.${match[2]}.${match[1]}` : isoDay;
}
