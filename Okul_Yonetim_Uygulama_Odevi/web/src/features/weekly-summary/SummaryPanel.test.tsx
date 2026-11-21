import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import type { Route } from "../admin/adminFake";
import { basis, classA, drafted, json, panel, studentList, teacherMe, validationProblem, weeklyFake } from "./weeklySummaryFake";

// Spec 0014 AC-2, AC-3, AC-8, AC-11 ve AC-12 (web, öğrenci paneli): dayanak (yoklama sayıları ve günleri, ödev, not, dayanak
// satırı); dayanak boşsa "Bu hafta için özetlenecek veri yok." ve oluştur not yazılınca açılır; "Oluşturuluyor…" duyurulur ve
// düğmeler kilitli; "AI taslağı — göndermeden önce kontrol edin" etiketli düzenlenebilir taslak; alan hataları (not 501, metin boş /
// 1501) alanla ilişkili, duyurulur ve istek gitmez (500 ve 1500 gider); sunucu hataları duyurulur; "Onayla ve gönder" → "Gönderildi"
// (salt okunur) — gönderim paneldeki taslağın kimliğini ve oluşturulma zamanını taşır, taslak değiştiyse "Taslak değişti; paneli
// yenileyin." duyurulur (review R1-2); "Vazgeç" taslağı siler; iki tema.
afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = "";
  window.localStorage.clear();
});

function start(routes: Route[]) {
  const api = weeklyFake([
    ["GET", /^\/api\/weekly-summaries\/classrooms$/, () => json(200, [classA])],
    ["GET", /^\/api\/weekly-summaries$/, () => json(200, studentList())],
    ...routes,
  ], teacherMe);
  vi.stubGlobal("fetch", api.fetch);
  window.location.hash = "#/haftalik-ozet";
  render(<App />);
  return api;
}

async function openAli() {
  await userEvent.click(await screen.findByRole("button", { name: "Ali Yılmaz, Özet yok, Aç" }));
  return screen.findByRole("heading", { name: "Ali Yılmaz · 16–22 Kasım" });
}

const student: RegExp = /^\/api\/weekly-summaries\/students\/s1$/;
const draft: RegExp = /^\/api\/weekly-summaries\/students\/s1\/draft$/;
const send: RegExp = /^\/api\/weekly-summaries\/students\/s1\/send$/;

function setValue(element: HTMLElement, value: string) {
  fireEvent.change(element, { target: { value } });
}

describe.each(["light", "dark"] as const)("%s tema", (mode) => {
  beforeEach(() => window.localStorage.setItem(THEME_STORAGE_KEY, mode));
  afterEach(() => expect(document.documentElement.getAttribute("data-theme")).toBe(mode));

  it("dayanak gösterilir; oluştur → 'Oluşturuluyor…' (kilitli, duyurulur) → AI taslağı etiketli düzenlenebilir metin", async () => {
    let release: (response: Response) => void = () => {};
    const held = new Promise<Response>((resolve) => {
      release = resolve;
    });
    const api = start([
      ["GET", student, () => json(200, panel())],
      ["POST", draft, () => held],
    ]);
    await openAli();

    expect(screen.getByText("Yoklama: 5 gün alındı — 3 gün zamanında, 1 gün geç geldi, 1 gün gelmedi.")).toBeInTheDocument();
    expect(screen.getByText("Gelmediği günler: 18.11.2026")).toBeInTheDocument();
    expect(screen.getByText("Geç geldiği günler: 19.11.2026")).toBeInTheDocument();
    expect(screen.getByText("Matematik — Kesirler (teslim 20.11.2026): Teslim etti")).toBeInTheDocument();
    expect(screen.getByText("Türkçe yazılı — 1. yazılı (17.11.2026): 85")).toBeInTheDocument();
    expect(screen.getByText("Dayanak: 4 gün geldi (1 gün geç), 1 gün gelmedi; 1 ödev; 1 not")).toBeInTheDocument();
    expect(screen.queryByText("AI taslağı — göndermeden önce kontrol edin")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Özet oluştur" }));
    expect(await screen.findByText("Oluşturuluyor…", { selector: "p" })).toHaveAttribute("role", "status");
    expect(screen.getByRole("button", { name: "Oluşturuluyor…" })).toBeDisabled();
    expect(screen.getByRole("textbox", { name: /Öğretmen notu/ })).toBeDisabled();
    expect(api.requests.filter((r) => r.method === "POST" && r.path.endsWith("/draft"))).toHaveLength(1);
    release(json(200, drafted()));

    expect(await screen.findByText("AI taslağı — göndermeden önce kontrol edin")).toBeInTheDocument();
    const text = screen.getByRole("textbox", { name: /Özet metni/ });
    expect(text).toHaveValue("Ali bu hafta dört gün okuldaydı.");
    expect(text).toBeEnabled();
    expect(screen.getByRole("button", { name: "Yeniden oluştur" })).toBeEnabled();
    expect(api.requests.find((r) => r.method === "POST" && r.path.endsWith("/draft"))?.body).toEqual({ teacherNote: null });
  });

  it("dayanak boş: 'veri yok' ve oluştur kapalı; not yazınca açılır, not gövdede gider", async () => {
    const api = start([
      ["GET", student, () => json(200, panel({ hasData: false, basisLine: "Dayanak: yoklama yok", basis: basis({ attendance: { daysTaken: 0, present: 0, absent: 0, late: 0, absentDays: [], lateDays: [] }, assignments: [], grades: [] }) }))],
      ["POST", draft, () => json(200, drafted())],
    ]);
    await openAli();

    expect(screen.getByText("Bu hafta için özetlenecek veri yok.")).toHaveAttribute("role", "status");
    expect(screen.getByText("Bu hafta yoklama kaydı yok.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Özet oluştur" })).toBeDisabled();
    await userEvent.type(screen.getByRole("textbox", { name: /Öğretmen notu/ }), "Kitap okudu.");
    await userEvent.click(screen.getByRole("button", { name: "Özet oluştur" }));
    await screen.findByText("AI taslağı — göndermeden önce kontrol edin");
    expect(api.requests.find((r) => r.method === "POST" && r.path.endsWith("/draft"))?.body).toEqual({ teacherNote: "Kitap okudu." });
  });

  it("not 501 alan hatası (istek gitmez), 500 gider; sunucunun 'kullanılamıyor' mesajı duyurulur", async () => {
    const api = start([
      ["GET", student, () => json(200, panel())],
      ["POST", draft, () => json(400, { title: "Kural", status: 400, detail: "Yapay zekâ şu an kullanılamıyor. Daha sonra tekrar deneyin." })],
    ]);
    await openAli();
    const note = screen.getByRole("textbox", { name: /Öğretmen notu/ });

    setValue(note, "n".repeat(501));
    await userEvent.click(screen.getByRole("button", { name: "Özet oluştur" }));
    const error = await screen.findByText("Öğretmen notu en fazla 500 karakter olabilir");
    expect(error).toHaveAttribute("role", "alert");
    expect(note).toHaveAttribute("aria-invalid", "true");
    expect(note).toHaveAttribute("aria-describedby", error.id);
    expect(api.requests.filter((r) => r.method === "POST" && r.path.endsWith("/draft"))).toHaveLength(0);

    setValue(note, "n".repeat(500));
    await userEvent.click(screen.getByRole("button", { name: "Özet oluştur" }));
    expect(await screen.findByText("Yapay zekâ şu an kullanılamıyor. Daha sonra tekrar deneyin.")).toHaveAttribute("role", "alert");
    expect(api.requests.filter((r) => r.method === "POST" && r.path.endsWith("/draft"))).toHaveLength(1);
    expect(screen.queryByText("Öğretmen notu en fazla 500 karakter olabilir")).not.toBeInTheDocument();
  });

  it("metin boş ya da 1501 alan hatası (istek gitmez); 1500 gönderilir → 'Gönderildi' salt okunur", async () => {
    let sent = false;
    const finalText = "m".repeat(1500);
    const api = start([
      ["GET", student, () => json(200, sent ? panel({ status: "Sent", finalText, draftText: "x", sentAt: "2026-11-20T09:05:00Z" }) : drafted())],
      ["POST", send, () => {
        sent = true;
        return json(200, { draftId: "d1", notifiedGuardians: 2, sentAt: "2026-11-20T09:05:00Z" });
      }],
    ]);
    await openAli();
    const text = await screen.findByRole("textbox", { name: /Özet metni/ });

    setValue(text, "   ");
    await userEvent.click(screen.getByRole("button", { name: "Onayla ve gönder" }));
    expect(await screen.findByText("Özet metni boş olamaz")).toHaveAttribute("role", "alert");
    expect(text).toHaveAttribute("aria-invalid", "true");
    setValue(text, "m".repeat(1501));
    await userEvent.click(screen.getByRole("button", { name: "Onayla ve gönder" }));
    expect(await screen.findByText("Özet metni en fazla 1500 karakter olabilir")).toBeInTheDocument();
    expect(api.requests.filter((r) => r.path.endsWith("/send"))).toHaveLength(0);

    setValue(text, finalText);
    await userEvent.click(screen.getByRole("button", { name: "Onayla ve gönder" }));
    expect(await screen.findByText("Özet 2 veliye gönderildi.")).toHaveAttribute("role", "status");
    expect(await screen.findByRole("heading", { name: "Gönderildi" })).toBeInTheDocument();
    expect(screen.getByText(finalText)).toBeInTheDocument();
    expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Özet oluştur" })).not.toBeInTheDocument();
    expect(api.requests.find((r) => r.path.endsWith("/send"))?.body).toEqual({ text: finalText, draftId: "d1", draftGeneratedAt: "2026-11-20T09:01:00.123456Z" });
  });

  it("sunucu hataları duyurulur: zaten gönderildi, velisi yok, gerçek doğrulama gövdesi", async () => {
    const replies = [
      json(400, { title: "Kural", status: 400, detail: "Bu hafta için özet zaten gönderildi." }),
      json(400, { title: "Kural", status: 400, detail: "Bu öğrencinin kayıtlı velisi yok." }),
      json(400, validationProblem("Text", "Özet metni en fazla 1500 karakter olabilir")),
    ];
    start([
      ["GET", student, () => json(200, drafted())],
      ["POST", send, () => replies.shift()!],
    ]);
    await openAli();

    for (const message of ["Bu hafta için özet zaten gönderildi.", "Bu öğrencinin kayıtlı velisi yok.", "Özet metni en fazla 1500 karakter olabilir"]) {
      await userEvent.click(await screen.findByRole("button", { name: "Onayla ve gönder" }));
      expect(await screen.findByText(message)).toHaveAttribute("role", "alert");
    }
  });

  it("gönderim gördüğü taslağa bağlı: taslak değiştiyse sunucunun mesajı duyurulur, 'Gönderildi' olmaz", async () => {
    const api = start([
      ["GET", student, () => json(200, drafted())],
      ["POST", send, () => json(400, { title: "Kural", status: 400, detail: "Taslak değişti; paneli yenileyin." })],
    ]);
    await openAli();

    await userEvent.click(await screen.findByRole("button", { name: "Onayla ve gönder" }));
    expect(await screen.findByText("Taslak değişti; paneli yenileyin.")).toHaveAttribute("role", "alert");
    expect(screen.queryByRole("heading", { name: "Gönderildi" })).not.toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: /Özet metni/ })).toHaveValue("Ali bu hafta dört gün okuldaydı.");
    expect(api.requests.filter((r) => r.path.endsWith("/send")).map((r) => r.body)).toEqual([
      { text: "Ali bu hafta dört gün okuldaydı.", draftId: "d1", draftGeneratedAt: "2026-11-20T09:01:00.123456Z" },
    ]);
  });

  it("Vazgeç taslağı siler, panel 'Özet yok'a döner", async () => {
    let discarded = false;
    const api = start([
      ["GET", student, () => json(200, discarded ? panel() : drafted())],
      ["DELETE", draft, () => {
        discarded = true;
        return new Response(null, { status: 204 });
      }],
    ]);
    await openAli();

    await userEvent.click(await screen.findByRole("button", { name: "Vazgeç" }));
    expect(await screen.findByText("Taslak silindi.")).toHaveAttribute("role", "status");
    expect(await screen.findByRole("button", { name: "Özet oluştur" })).toBeInTheDocument();
    expect(screen.queryByText("AI taslağı — göndermeden önce kontrol edin")).not.toBeInTheDocument();
    expect(api.requests.filter((r) => r.method === "DELETE")).toHaveLength(1);
  });

  it("panel yüklenemezse hata + tekrar dene", async () => {
    let calls = 0;
    start([["GET", student, () => (++calls === 1 ? json(404, { detail: "Öğrenci bulunamadı" }) : json(200, panel()))]]);
    await userEvent.click(await screen.findByRole("button", { name: "Ali Yılmaz, Özet yok, Aç" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Öğrenci bulunamadı");
    await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));
    expect(await screen.findByRole("heading", { name: "Ali Yılmaz · 16–22 Kasım" })).toBeInTheDocument();
  });
});
