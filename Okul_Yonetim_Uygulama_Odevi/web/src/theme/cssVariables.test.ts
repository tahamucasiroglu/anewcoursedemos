import { colorRoles, themeColors, type ColorScheme } from "@okul/theme";
import indexCss from "../index.css?raw";
import themeCss from "./theme.css?raw";

// Spec 0002, AC-1: web'in CSS renk değişkenleri ortak tema (tokens.json) ile birebir aynıdır; bileşen stillerinde
// renk değeri yazılmaz (renk tek kaynaktan gelir).
function block(selector: string): string {
  const start = themeCss.indexOf(selector);
  expect(start, `${selector} bloğu theme.css'te olmalı`).toBeGreaterThanOrEqual(0);
  return themeCss.slice(start, themeCss.indexOf("}", start));
}

function cssName(role: string): string {
  return `--color-${role.replace(/[A-Z]/g, (c) => `-${c.toLowerCase()}`)}`;
}

const selectors: Record<ColorScheme, string> = {
  light: ':root[data-theme="light"]',
  dark: ':root[data-theme="dark"]',
};

describe.each<ColorScheme>(["light", "dark"])("%s mod CSS değişkenleri", (scheme) => {
  it.each(colorRoles)("%s = tokens.json", (role) => {
    expect(block(selectors[scheme])).toContain(`${cssName(role)}: ${themeColors[scheme][role]};`);
  });
});

it("index.css'te renk değeri yok, yalnızca değişken kullanılır", () => {
  expect(indexCss).not.toMatch(/#[0-9a-f]{3,8}\b|rgba?\(|hsla?\(/i);
});

it("tema seçici kabı odak çerçevesini kırpmaz (AC-11; review round 1, M-1)", () => {
  const start = indexCss.indexOf(".theme-switcher {");
  expect(start, ".theme-switcher kuralı index.css'te bulunmalı").toBeGreaterThanOrEqual(0);
  const rule = indexCss.slice(start, indexCss.indexOf("}", start));
  expect(rule).not.toMatch(/overflow\s*:\s*(hidden|clip)/);
});
