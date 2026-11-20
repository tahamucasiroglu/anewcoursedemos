// Oturum yönetimi (spec 0004): giriş, okul seçimi, yenileme, çıkış. Web ve mobil aynı kodu kullanır; farkı yalnızca
// yenileme anahtarının taşınmasıdır: web "cookie" (HttpOnly çerez, JavaScript görmez), mobil "body" (güvenli depoda).
// Erişim anahtarı yalnızca bellekte durur. Bir istek 401 alırsa oturum bir kez yenilenir ve istek tekrarlanır;
// yenileme sunucu tarafından reddedilirse oturum biter (onExpired) ve ekran giriş sayfasına döner. Sunucuya
// ulaşılamıyorsa oturum bitmiş sayılmaz (kayıtlı anahtar silinmez); istek ağ hatasıyla sonuçlanır (review L-2).
// Platform yöneticisinde (spec 0007) giriş oturum değil iki adımlı doğrulama beklemesi döner; oturum kodla açılır.
import createClient from "openapi-fetch";
import type { components, paths } from "../schema";

export type AuthTokens = components["schemas"]["AuthTokensResponse"];
export type Me = components["schemas"]["MeResponse"];
export type TenantSummary = components["schemas"]["TenantSummary"];
export type BranchSummary = components["schemas"]["BranchSummary"];

/** Mobilde yenileme anahtarının saklandığı yer (expo-secure-store); web'de kullanılmaz. */
export interface RefreshTokenStore {
  get(): Promise<string | null>;
  set(token: string | null): Promise<void>;
}

export type RefreshTokenDelivery = "cookie" | "body";

export type AuthOutcome =
  | { kind: "ok"; tokens: AuthTokens }
  | { kind: "twoFactor"; challenge: string; setupRequired: boolean }
  | { kind: "rejected"; status: number; message: string | null }
  | { kind: "unreachable" };

export interface AuthSessionOptions {
  baseUrl: string;
  delivery: RefreshTokenDelivery;
  store?: RefreshTokenStore;
  onExpired?: () => void;
}

const AUTH_PATHS = ["/api/auth/login", "/api/auth/refresh", "/api/auth/two-factor/setup", "/api/auth/two-factor/setup/confirm", "/api/auth/two-factor/verify"];

export type TwoFactorSetupInfo = components["schemas"]["TwoFactorSetupResult"];

export type TwoFactorSetupOutcome =
  | { kind: "ok"; setup: TwoFactorSetupInfo }
  | { kind: "rejected"; status: number; message: string | null }
  | { kind: "unreachable" };

export class AuthSession {
  readonly client;
  private readonly options: AuthSessionOptions;
  private accessToken: string | null = null;
  private refreshing: Promise<AuthOutcome["kind"]> | null = null;

  constructor(options: AuthSessionOptions) {
    this.options = options;
    this.client = createClient<paths>({ baseUrl: options.baseUrl, fetch: (request: Request) => this.send(request) });
  }

  get isSignedIn(): boolean {
    return this.accessToken !== null;
  }

  async login(email: string, password: string): Promise<AuthOutcome> {
    return this.authenticate(() =>
      this.client.POST("/api/auth/login", {
        body: { email, password, refreshTokenInBody: this.options.delivery === "body" },
      }),
    );
  }

  /** İki adımlı doğrulama kurulumu (spec 0007): doğrulayıcı uygulamaya eklenecek anahtar ve kare kod adresi. */
  async beginTwoFactorSetup(challenge: string): Promise<TwoFactorSetupOutcome> {
    try {
      const { data, error, response } = await this.client.POST("/api/auth/two-factor/setup", { body: { challenge } });
      return data ? { kind: "ok", setup: data } : { kind: "rejected", status: response.status, message: problemMessage(error) };
    } catch {
      return { kind: "unreachable" };
    }
  }

  /** 6 haneli kod: kurulumu tamamlar (setup) ya da girişi doğrular; doğruysa oturum açılır. */
  async completeTwoFactor(challenge: string, code: string, setup: boolean): Promise<AuthOutcome> {
    const body = { challenge, code, refreshTokenInBody: this.options.delivery === "body" };
    return this.authenticate(() =>
      setup ? this.client.POST("/api/auth/two-factor/setup/confirm", { body }) : this.client.POST("/api/auth/two-factor/verify", { body }),
    );
  }

  /** Uygulama açılışında: kayıtlı oturum varsa sessizce yeniler (web: çerez, mobil: güvenli depo). */
  async resume(): Promise<AuthOutcome> {
    if (this.options.delivery === "body" && !(await this.options.store?.get())) {
      return { kind: "rejected", status: 401, message: null };
    }

    return this.refreshOutcome();
  }

  async selectTenant(tenantId: string): Promise<AuthOutcome> {
    return this.authenticate(() => this.client.POST("/api/auth/select-tenant", { body: { tenantId } }));
  }

  async me(): Promise<Me | null> {
    const { data } = await this.client.GET("/api/me");
    return data ?? null;
  }

  async logout(): Promise<void> {
    try {
      await this.client.POST("/api/auth/logout");
    } catch {
      // Sunucuya ulaşılamasa da yerel oturum kapanır.
    } finally {
      this.accessToken = null;
      await this.options.store?.set(null);
    }
  }

  private async authenticate(call: () => Promise<{ data?: AuthTokens; error?: unknown; response: Response }>): Promise<AuthOutcome> {
    try {
      const { data, error, response } = await call();
      if (data?.twoFactorChallenge) {
        // Platform yöneticisi (spec 0007): şifre doğru, oturum kod doğrulanınca açılır.
        return { kind: "twoFactor", challenge: data.twoFactorChallenge, setupRequired: data.twoFactorSetupRequired };
      }

      if (data) {
        await this.accept(data);
        return { kind: "ok", tokens: data };
      }

      return { kind: "rejected", status: response.status, message: problemMessage(error) };
    } catch {
      return { kind: "unreachable" };
    }
  }

  private async accept(tokens: AuthTokens): Promise<void> {
    this.accessToken = tokens.accessToken;
    if (this.options.delivery === "body" && tokens.refreshToken) {
      await this.options.store?.set(tokens.refreshToken);
    }
  }

  /**
   * Yenileme sonucu: yalnızca sunucunun açık reddi (401/400/403) oturumu bitirir. Ağ geçidi/sunucu hatası (5xx, ör. Pi'de
   * API yeniden başlarken nginx 502) ve 429 geçicidir: "ulaşılamıyor" sayılır, kayıtlı oturum silinmez (review L-6).
   */
  private async refreshOutcome(): Promise<AuthOutcome> {
    const outcome = await this.authenticate(() => this.postRefresh());
    return outcome.kind === "rejected" && (outcome.status >= 500 || outcome.status === 429) ? { kind: "unreachable" } : outcome;
  }

  private async postRefresh() {
    const refreshToken = this.options.delivery === "body" ? await this.options.store?.get() : null;
    return this.client.POST("/api/auth/refresh", { body: refreshToken ? { refreshToken } : {} });
  }

  /** Oturumu bir kez yeniler; aynı anda gelen 401'ler aynı yenilemeyi bekler. Sonuç: "ok" | "rejected" | "unreachable". */
  private refresh(): Promise<AuthOutcome["kind"]> {
    this.refreshing ??= (async () => {
      const outcome = await this.refreshOutcome();
      if (outcome.kind === "rejected") {
        this.accessToken = null;
        await this.options.store?.set(null);
        this.options.onExpired?.();
      }

      return outcome.kind;
    })().finally(() => {
      this.refreshing = null;
    });
    return this.refreshing;
  }

  private async send(request: Request): Promise<Response> {
    const retry = request.clone();
    const isAuthPath = AUTH_PATHS.some((path) => new URL(request.url, "http://x").pathname === path);
    const response = await globalThis.fetch(this.withToken(request));
    if (response.status !== 401 || isAuthPath || this.accessToken === null) {
      return response;
    }

    const refreshed = await this.refresh();
    if (refreshed === "unreachable") {
      throw new TypeError("Sunucuya ulaşılamıyor");
    }

    return refreshed === "ok" ? globalThis.fetch(this.withToken(retry)) : response;
  }

  private withToken(request: Request): Request {
    if (this.accessToken === null) {
      return request;
    }

    const headers = new Headers(request.headers);
    headers.set("Authorization", `Bearer ${this.accessToken}`);
    return new Request(request, { headers });
  }
}

function problemMessage(error: unknown): string | null {
  if (error && typeof error === "object" && "detail" in error && typeof error.detail === "string") {
    return error.detail;
  }

  return null;
}
