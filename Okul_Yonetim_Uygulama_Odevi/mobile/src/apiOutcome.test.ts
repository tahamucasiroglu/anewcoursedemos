import { callApi, problemFields, problemMessage } from "@okul/api-client";

// Review round 1 (spec 0013), R1-1 / T20: nArchitecture'ın doğrulama gövdesi `detail`'de sabit İngilizce metin, Türkçe alan
// mesajları `Errors` DİZİSİNDE (`{ Property, Errors: [...] }`, büyük harfle) — sözlük değil. İstemci alan mesajını gösterir;
// sunucunun `detail`'e kopyaladığı Türkçe mesaja güvenmeden de doğru metni bulur. api-client'ın kendi test adımı yok (plan).
const english = "One or more validation errors occurred.";
const nArchitectureBody = {
  Errors: [{ Property: "Date", Errors: ["Tarih sınıfın dönemi içinde olmalı"] }],
  type: "https://example.com/probs/validation",
  title: "Validation error(s)",
  status: 400,
  detail: english,
};

describe("apiOutcome", () => {
  it("nArchitecture dizisi (büyük harf): İngilizce detail yerine Türkçe alan mesajı", () => {
    expect(problemMessage(nArchitectureBody)).toBe("Tarih sınıfın dönemi içinde olmalı");
    expect(problemFields(nArchitectureBody)).toEqual([{ property: "Date", message: "Tarih sınıfın dönemi içinde olmalı" }]);
  });

  it("dizi küçük harfle ve ASP.NET sözlüğü de okunur; alan yoksa detail", () => {
    expect(problemMessage({ errors: [{ property: "Grades[s2].Score", errors: ["Puan 0 ile 100 arasında tam sayı olmalı"] }], detail: english })).toBe(
      "Puan 0 ile 100 arasında tam sayı olmalı",
    );
    expect(problemFields({ errors: { Title: ["Başlık zorunludur"] } })).toEqual([{ property: "Title", message: "Başlık zorunludur" }]);
    expect(problemMessage({ detail: "Taslak bulunamadı" })).toBe("Taslak bulunamadı");
    expect(problemMessage({ Errors: [{ Property: "Date", Errors: [] }], detail: "" })).toBeNull();
    expect(problemMessage("düz metin")).toBeNull();
  });

  it("callApi: 400 doğrulama yanıtı Türkçe mesaj ve alan listesiyle döner", async () => {
    const response = new Response(null, { status: 400 });
    const outcome = await callApi(Promise.resolve({ error: nArchitectureBody, response }));
    expect(outcome).toEqual({
      ok: false,
      unreachable: false,
      status: 400,
      message: "Tarih sınıfın dönemi içinde olmalı",
      fields: [{ property: "Date", message: "Tarih sınıfın dönemi içinde olmalı" }],
    });
  });
});
