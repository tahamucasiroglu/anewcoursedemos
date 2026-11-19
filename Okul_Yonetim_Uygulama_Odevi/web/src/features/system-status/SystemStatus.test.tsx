import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SystemStatus } from "./SystemStatus";

// Testler ağ katmanını taklit eder; ekran ortak api-client üzerinden gerçek isteği yapar (spec 0001, AC-3).
const fetchMock = vi.fn<(input: Request) => Promise<Response>>();

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });
}

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("SystemStatus", () => {
  it("açılışta yükleniyor durumunu gösterir", () => {
    fetchMock.mockReturnValue(new Promise(() => {}));
    render(<SystemStatus />);
    expect(screen.getByRole("status")).toHaveTextContent("Sistem durumu kontrol ediliyor…");
  });

  it("API çalışıyorsa API'nin Türkçe mesajını gösterir, Tekrar dene yoktur", async () => {
    fetchMock.mockResolvedValue(json(200, { status: "healthy", message: "Sistem çalışıyor" }));
    render(<SystemStatus />);
    expect(await screen.findByText(/Sistem çalışıyor/)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Tekrar dene" })).not.toBeInTheDocument();
  });

  it("503 gelirse hizmet dışı mesajını ve Tekrar dene düğmesini gösterir", async () => {
    fetchMock.mockResolvedValue(json(503, { status: "unavailable", message: "Sistem şu anda hizmet dışı" }));
    render(<SystemStatus />);
    expect(await screen.findByText("Sistem şu anda hizmet dışı")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Tekrar dene" })).toBeInTheDocument();
  });

  it("API'ye ulaşılamazsa ulaşılamıyor mesajını gösterir", async () => {
    fetchMock.mockRejectedValue(new TypeError("Failed to fetch"));
    render(<SystemStatus />);
    expect(await screen.findByText("Sunucuya ulaşılamıyor. Lütfen daha sonra tekrar deneyin.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Tekrar dene" })).toBeInTheDocument();
  });

  it("Tekrar dene yeniden sorgular ve sonucu gösterir", async () => {
    fetchMock.mockRejectedValueOnce(new TypeError("Failed to fetch"));
    fetchMock.mockResolvedValueOnce(json(200, { status: "healthy", message: "Sistem çalışıyor" }));
    render(<SystemStatus />);

    await userEvent.click(await screen.findByRole("button", { name: "Tekrar dene" }));

    expect(await screen.findByText(/Sistem çalışıyor/)).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
