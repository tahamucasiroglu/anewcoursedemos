// Türkçe metinler tr.json'dan gelir; koda gömülmez (docs/conventions.md, Language). Yeni dil = yeni JSON.
import tr from "./tr.json";

export type TextKey = keyof typeof tr;

export function t(key: TextKey): string {
  return tr[key];
}
