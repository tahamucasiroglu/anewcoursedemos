import { fireEvent, render, screen } from "@testing-library/react-native";
import { SystemStatusScreen } from "./SystemStatusScreen";

// Testler ağ katmanını taklit eder; ekran ortak api-client üzerinden gerçek isteği yapar (spec 0001, AC-4).
const fetchMock = jest.fn<Promise<Response>, [Request]>();
const originalFetch = globalThis.fetch;

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });
}

beforeEach(() => {
  fetchMock.mockReset();
  globalThis.fetch = fetchMock as unknown as typeof fetch;
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

describe("SystemStatusScreen", () => {
  it("açılışta yükleniyor durumunu gösterir", async () => {
    fetchMock.mockReturnValue(new Promise(() => {}));
    await render(<SystemStatusScreen />);
    expect(screen.getByText("Sistem durumu kontrol ediliyor…")).toBeTruthy();
  });

  it("API çalışıyorsa API'nin Türkçe mesajını gösterir, Tekrar dene yoktur", async () => {
    fetchMock.mockResolvedValue(json(200, { status: "healthy", message: "Sistem çalışıyor" }));
    await render(<SystemStatusScreen />);
    expect(await screen.findByText(/Sistem çalışıyor/)).toBeTruthy();
    expect(screen.queryByText("Tekrar dene")).toBeNull();
  });

  it("503 gelirse hizmet dışı mesajını ve Tekrar dene düğmesini gösterir", async () => {
    fetchMock.mockResolvedValue(json(503, { status: "unavailable", message: "Sistem şu anda hizmet dışı" }));
    await render(<SystemStatusScreen />);
    expect(await screen.findByText("Sistem şu anda hizmet dışı")).toBeTruthy();
    expect(screen.getByText("Tekrar dene")).toBeTruthy();
  });

  it("API'ye ulaşılamazsa ulaşılamıyor mesajını gösterir", async () => {
    fetchMock.mockRejectedValue(new TypeError("Network request failed"));
    await render(<SystemStatusScreen />);
    expect(await screen.findByText("Sunucuya ulaşılamıyor. Lütfen daha sonra tekrar deneyin.")).toBeTruthy();
    expect(screen.getByText("Tekrar dene")).toBeTruthy();
  });

  it("Tekrar dene basınca yeniden yükleniyor durumuna döner", async () => {
    fetchMock.mockRejectedValueOnce(new TypeError("Network request failed"));
    fetchMock.mockReturnValueOnce(new Promise(() => {}));
    await render(<SystemStatusScreen />);

    await fireEvent.press(await screen.findByText("Tekrar dene"));

    expect(screen.getByText("Sistem durumu kontrol ediliyor…")).toBeTruthy();
    expect(screen.queryByText("Tekrar dene")).toBeNull();
  });

  it("hizmet dışından Tekrar dene ile çalışıyor durumuna geçer", async () => {
    fetchMock.mockResolvedValueOnce(json(503, { status: "unavailable", message: "Sistem şu anda hizmet dışı" }));
    fetchMock.mockResolvedValueOnce(json(200, { status: "healthy", message: "Sistem çalışıyor" }));
    await render(<SystemStatusScreen />);

    await fireEvent.press(await screen.findByText("Tekrar dene"));

    expect(await screen.findByText(/Sistem çalışıyor/)).toBeTruthy();
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("Tekrar dene yeniden sorgular ve sonucu gösterir", async () => {
    fetchMock.mockRejectedValueOnce(new TypeError("Network request failed"));
    fetchMock.mockResolvedValueOnce(json(200, { status: "healthy", message: "Sistem çalışıyor" }));
    await render(<SystemStatusScreen />);

    await fireEvent.press(await screen.findByText("Tekrar dene"));

    expect(await screen.findByText(/Sistem çalışıyor/)).toBeTruthy();
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
