import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";

// Testler ağ katmanını taklit eder; ekran ortak api-client üzerinden gerçek isteği yapar (spec 0001, AC-3).
// Spec 0002, AC-10: aynı davranış açık ve koyu modda, uygulamanın içinde (tema uygulanmış hâlde) sınanır.
const fetchMock = vi.fn<(input: Request) => Promise<Response>>();

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });
}

beforeEach(() => {
  fetchMock.mockReset();
  // Spec 0004: uygulama açılışta oturumu da dener (/api/auth/...); o istekler "oturum yok" (401) alır ve bu testlerin
  // sayaçlarına/sırasına karışmaz — fetchMock yalnızca sistem durumu isteklerini görür.
  vi.stubGlobal("fetch", (request: Request) =>
    new URL(request.url, "http://x").pathname.startsWith("/api/auth/") ? Promise.resolve(json(401, {})) : fetchMock(request),
  );
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe.each(["light", "dark"] as const)("SystemStatus — %s mod", (mode) => {
  beforeEach(() => {
    window.localStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    window.localStorage.clear();
    expect(document.documentElement.getAttribute("data-theme")).toBe(mode);
  });

  it("açılışta yükleniyor durumunu gösterir", () => {
    fetchMock.mockReturnValue(new Promise(() => {}));
    render(<App />);
    expect(screen.getByRole("status")).toHaveTextContent("Sistem durumu kontrol ediliyor…");
  });

  it("API çalışıyorsa API'nin Türkçe mesajını gösterir, Tekrar dene yoktur", async () => {
    fetchMock.mockResolvedValue(json(200, { status: "healthy", message: "Sistem çalışıyor" }));
    render(<App />);
    expect(await screen.findByText(/Sistem çalışıyor/)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Tekrar dene" })).not.toBeInTheDocument();
  });

  it("503 gelirse hizmet dışı mesajını ve Tekrar dene düğmesini gösterir", async () => {
    fetchMock.mockResolvedValue(json(503, { status: "unavailable", message: "Sistem şu anda hizmet dışı" }));
    render(<App />);
    expect(await screen.findByText("Sistem şu anda hizmet dışı")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Tekrar dene" })).toBeInTheDocument();
  });

  it("API'ye ulaşılamazsa ulaşılamıyor mesajını gösterir", async () => {
    fetchMock.mockRejectedValue(new TypeError("Failed to fetch"));
    render(<App />);
    expect(await screen.findByText("Sunucuya ulaşılamıyor. Lütfen daha sonra tekrar deneyin.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Tekrar dene" })).toBeInTheDocument();
  });

  it("Tekrar dene basınca yeniden yükleniyor durumuna döner", async () => {
    fetchMock.mockRejectedValueOnce(new TypeError("Failed to fetch"));
    fetchMock.mockReturnValueOnce(new Promise(() => {}));
    render(<App />);

    await userEvent.click(await screen.findByRole("button", { name: "Tekrar dene" }));

    expect(screen.getByRole("status")).toHaveTextContent("Sistem durumu kontrol ediliyor…");
    expect(screen.queryByRole("button", { name: "Tekrar dene" })).not.toBeInTheDocument();
  });

  it("hizmet dışından Tekrar dene ile çalışıyor durumuna geçer", async () => {
    fetchMock.mockResolvedValueOnce(json(503, { status: "unavailable", message: "Sistem şu anda hizmet dışı" }));
    fetchMock.mockResolvedValueOnce(json(200, { status: "healthy", message: "Sistem çalışıyor" }));
    render(<App />);

    await userEvent.click(await screen.findByRole("button", { name: "Tekrar dene" }));

    expect(await screen.findByText(/Sistem çalışıyor/)).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("Tekrar dene yeniden sorgular ve sonucu gösterir", async () => {
    fetchMock.mockRejectedValueOnce(new TypeError("Failed to fetch"));
    fetchMock.mockResolvedValueOnce(json(200, { status: "healthy", message: "Sistem çalışıyor" }));
    render(<App />);

    await userEvent.click(await screen.findByRole("button", { name: "Tekrar dene" }));

    expect(await screen.findByText(/Sistem çalışıyor/)).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
