import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { THEME_STORAGE_KEY } from "@okul/theme";
import App from "../../App";
import { fakeApi, json, me, tokens } from "./fakeApi";

// Spec 0004 AC-3, AC-5, AC-6, AC-13 (web): giriş ekranı uygulamanın içinde, iki temada; ağ taklit edilir.
const noSession = () => json(401, { detail: "Oturumunuz sona erdi, tekrar giriş yapın." });

afterEach(() => {
  vi.unstubAllGlobals();
  window.localStorage.clear();
});

async function fill(email: string, password: string) {
  if (email) {
    await userEvent.type(await screen.findByLabelText("E-posta"), email);
  }

  if (password) {
    await userEvent.type(screen.getByLabelText("Şifre"), password);
  }
}

describe.each(["light", "dark"] as const)("Giriş — %s tema", (mode) => {
  beforeEach(() => {
    window.localStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(document.documentElement.getAttribute("data-theme")).toBe(mode);
  });

  it("oturum yoksa boş giriş formu açılır", async () => {
    const api = fakeApi({ "POST /api/auth/refresh": noSession });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);

    expect(await screen.findByRole("button", { name: "Giriş yap" })).toBeEnabled();
    expect(screen.getByLabelText("E-posta")).toHaveValue("");
    expect(screen.queryByText("Oturumunuz sona erdi, tekrar giriş yapın.")).not.toBeInTheDocument();
  });

  it("doğru bilgiyle tek okullu kişi hoş geldin ekranını görür (ad, okul, rol, şube)", async () => {
    const api = fakeApi({
      "POST /api/auth/refresh": noSession,
      "POST /api/auth/login": () => json(200, tokens()),
      "GET /api/me": () => json(200, me()),
    });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);

    await fill("ayse@ornek.test", "Gizli-Sifre-1");
    await userEvent.click(screen.getByRole("button", { name: "Giriş yap" }));

    expect(await screen.findByRole("heading", { name: "Hoş geldiniz, Ayşe Kurucu" })).toBeInTheDocument();
    expect(screen.getByText("A Koleji")).toBeInTheDocument();
    expect(screen.getByText("Kurucu / Yönetici")).toBeInTheDocument();
    expect(screen.getByText("Merkez")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Okul değiştir" })).not.toBeInTheDocument();
    const login = api.requests.find((r) => r.url.endsWith("/api/auth/login"))!;
    expect(await login.json()).toEqual({ email: "ayse@ornek.test", password: "Gizli-Sifre-1", refreshTokenInBody: false });
  });

  it.each([
    ["", "Bir-sifre-1", "E-posta zorunludur"],
    ["gecersiz", "Bir-sifre-1", "Geçerli bir e-posta girin"],
    ["biri@ornek.test", "", "Şifre zorunludur"],
  ])("alan hatası (%s / %s) gösterilir ve istek gönderilmez", async (email, password, message) => {
    const api = fakeApi({ "POST /api/auth/refresh": noSession });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);
    await screen.findByRole("button", { name: "Giriş yap" });

    await fill(email, password);
    await userEvent.click(screen.getByRole("button", { name: "Giriş yap" }));

    const error = await screen.findByText(message);
    expect(error).toHaveAttribute("role", "alert");
    expect(api.paths()).not.toContain("POST /api/auth/login");
  });

  it("hatalı bilgide sunucunun tek tip mesajını duyurur", async () => {
    const api = fakeApi({
      "POST /api/auth/refresh": noSession,
      "POST /api/auth/login": () => json(401, { detail: "E-posta veya şifre hatalı" }),
    });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);

    await fill("ayse@ornek.test", "Yanlis-1");
    await userEvent.click(screen.getByRole("button", { name: "Giriş yap" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("E-posta veya şifre hatalı");
  });

  it("kilitliyken kilit mesajını gösterir", async () => {
    const api = fakeApi({
      "POST /api/auth/refresh": noSession,
      "POST /api/auth/login": () => json(401, { detail: "Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin." }),
    });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);

    await fill("ayse@ornek.test", "Gizli-Sifre-1");
    await userEvent.click(screen.getByRole("button", { name: "Giriş yap" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin.");
  });

  it("gönderilirken düğme kilitlidir ve 'Giriş yapılıyor…' yazar", async () => {
    const api = fakeApi({ "POST /api/auth/refresh": noSession, "POST /api/auth/login": () => new Promise<Response>(() => {}) });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);

    await fill("ayse@ornek.test", "Gizli-Sifre-1");
    await userEvent.click(screen.getByRole("button", { name: "Giriş yap" }));

    expect(await screen.findByRole("button", { name: "Giriş yapılıyor…" })).toBeDisabled();
  });

  it("sunucuya ulaşılamıyorsa mesaj ve 'Tekrar dene' gösterir; tekrar dene aynı bilgiyle yeniden dener", async () => {
    let attempts = 0;
    const api = fakeApi({
      "POST /api/auth/refresh": noSession,
      "POST /api/auth/login": () => {
        attempts++;
        return attempts === 1 ? Promise.reject(new TypeError("Failed to fetch")) : json(200, tokens());
      },
      "GET /api/me": () => json(200, me()),
    });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);

    await fill("ayse@ornek.test", "Gizli-Sifre-1");
    await userEvent.click(screen.getByRole("button", { name: "Giriş yap" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Sunucuya ulaşılamıyor. Lütfen daha sonra tekrar deneyin.");
    await userEvent.click(screen.getByRole("button", { name: "Tekrar dene" }));

    expect(await screen.findByRole("heading", { name: "Hoş geldiniz, Ayşe Kurucu" })).toBeInTheDocument();
    expect(attempts).toBe(2);
  });

  it("çerezdeki oturum geçerliyse açılışta doğrudan hoş geldin ekranı gelir", async () => {
    const api = fakeApi({ "POST /api/auth/refresh": () => json(200, tokens()), "GET /api/me": () => json(200, me()) });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);

    expect(await screen.findByRole("heading", { name: "Hoş geldiniz, Ayşe Kurucu" })).toBeInTheDocument();
    expect(screen.queryByLabelText("E-posta")).not.toBeInTheDocument();
  });
});

describe("Giriş — klavye ve oturum sonu", () => {
  it("akış yalnızca klavyeyle tamamlanır (Tab + Enter)", async () => {
    const api = fakeApi({
      "POST /api/auth/refresh": noSession,
      "POST /api/auth/login": () => json(200, tokens()),
      "GET /api/me": () => json(200, me()),
    });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);
    const email = await screen.findByLabelText("E-posta");

    email.focus();
    await userEvent.keyboard("ayse@ornek.test");
    await userEvent.tab();
    expect(screen.getByLabelText("Şifre")).toHaveFocus();
    await userEvent.keyboard("Gizli-Sifre-1{Enter}");

    expect(await screen.findByRole("heading", { name: "Hoş geldiniz, Ayşe Kurucu" })).toBeInTheDocument();
  });

  it("alan hatası alanla ilişkilidir (aria-invalid + aria-describedby)", async () => {
    vi.stubGlobal("fetch", fakeApi({ "POST /api/auth/refresh": noSession }).fetch);
    render(<App />);
    await userEvent.click(await screen.findByRole("button", { name: "Giriş yap" }));

    const email = screen.getByLabelText("E-posta");
    expect(email).toHaveAttribute("aria-invalid", "true");
    expect(email).toHaveAccessibleDescription("E-posta zorunludur");
  });

  it("oturum yenilenemezse giriş ekranına 'oturum sona erdi' ile döner", async () => {
    let meCalls = 0;
    const api = fakeApi({
      "POST /api/auth/refresh": () => (meCalls === 0 ? json(200, tokens()) : noSession()),
      "GET /api/me": () => {
        meCalls++;
        return json(401, {});
      },
    });
    vi.stubGlobal("fetch", api.fetch);
    render(<App />);

    expect(await screen.findByText("Oturumunuz sona erdi, tekrar giriş yapın.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Giriş yap" })).toBeInTheDocument();
    await waitFor(() => expect(api.paths().filter((p) => p === "POST /api/auth/refresh")).toHaveLength(2));
  });
});
