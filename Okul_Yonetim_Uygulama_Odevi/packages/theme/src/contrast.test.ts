import { contrastPairs, contrastRatio } from "./contrast";
import { colorRoles, themeColors, type ColorScheme } from "./tokens";

// Spec 0002, AC-9: iki modda tüm metin/zemin ve kenar çiftleri WCAG AA eşiğini geçer.
describe.each<ColorScheme>(["light", "dark"])("%s mod kontrastı", (scheme) => {
  const colors = themeColors[scheme];

  it.each(contrastPairs.map(([fg, bg, min]) => [fg, bg, min] as const))("%s / %s en az %s:1", (fg, bg, min) => {
    expect(contrastRatio(colors[fg], colors[bg])).toBeGreaterThanOrEqual(min);
  });

  it("iki modda aynı renk rolleri tanımlı", () => {
    expect(Object.keys(colors).sort()).toEqual([...colorRoles].sort());
  });
});

describe("contrastRatio", () => {
  it("siyah/beyaz 21:1, aynı renk 1:1", () => {
    expect(contrastRatio("#000000", "#ffffff")).toBeCloseTo(21, 5);
    expect(contrastRatio("#64748b", "#64748b")).toBeCloseTo(1, 5);
  });

  it("geçersiz renk hata verir", () => {
    expect(() => contrastRatio("mavi", "#ffffff")).toThrow();
  });
});
