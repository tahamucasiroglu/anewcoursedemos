import { useEffect, useState, type FormEvent } from "react";
import { validateMemberForm, type AuthSession, type BranchAdminView, type MemberDetail, type RoleInput } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "./adminApi";
import { failureText, fieldText } from "./adminText";
import { ConfirmDialog } from "./ConfirmDialog";
import { RolesEditor } from "./RolesEditor";
import { TempPasswordResult } from "./TempPasswordResult";

type Client = AuthSession["client"];
type CardState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; member: MemberDetail; activeFounders: number };

interface Props {
  client: Client;
  userId: string;
  branches: BranchAdminView[];
  onBack: () => void;
}

/**
 * Kişi kartı (spec 0005): bilgiler (ad-soyad, telefon düzenlenir; e-posta değişmez), roller ve şubeler, pasifleştir /
 * etkinleştir (onaylı), şifreyi sıfırla (onaylı → geçici şifre bir kez). Korumalarda düğme pasif ve nedeni yazılı:
 * kendini pasifleştirme, kendi yönetici rolünü alma, son etkin yönetici, başka okulda da kayıtlı kişinin şifresi.
 */
export function MemberCard({ client, userId, branches, onBack }: Props) {
  const [state, setState] = useState<CardState>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [profile, setProfile] = useState<{ fullName: string; phone: string } | null>(null);
  const [roles, setRoles] = useState<RoleInput[] | null>(null);
  const [confirm, setConfirm] = useState<"deactivate" | "reset" | null>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ error: boolean; text: string } | null>(null);
  const [temporary, setTemporary] = useState<string | null>(null);
  const [profileError, setProfileError] = useState<ReturnType<typeof validateMemberForm>>({});

  useEffect(() => {
    let active = true;
    Promise.all([
      call(client.GET("/api/admin/members/{userId}", { params: { path: { userId } } })),
      call(client.GET("/api/admin/members", { params: { query: { role: "Founder", status: "active" } } })),
    ]).then(([member, founders]) => {
      if (!active) {
        return;
      }

      if (member.ok) {
        setState({ kind: "ready", member: member.data, activeFounders: founders.ok ? Number(founders.data.total) : 1 });
        setProfile({ fullName: member.data.fullName, phone: member.data.phone ?? "" });
        setRoles(member.data.roles.map((r) => ({ role: r.role, branchIds: [...r.branchIds] })));
      } else {
        setState({ kind: "error", message: failureText(member) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, userId, reload]);

  const refresh = (text?: string) => {
    setState({ kind: "loading" });
    setMessage(text ? { error: false, text } : null);
    setReload((n) => n + 1);
  };

  if (temporary) {
    return <TempPasswordResult password={temporary} onDone={() => { setTemporary(null); refresh(); }} />;
  }

  if (state.kind === "loading") {
    return <p className="notice" aria-busy="true">{t("admin.loading")}</p>;
  }

  if (state.kind === "error") {
    return (
      <div className="card">
        <p className="form-error" role="alert">{state.message}</p>
        <div className="actions">
          <button type="button" onClick={() => refresh()}>{t("admin.retry")}</button>
          <button type="button" className="secondary" onClick={onBack}>{t("members.backToList")}</button>
        </div>
      </div>
    );
  }

  const member = state.member;
  const isFounder = member.roles.some((r) => r.role === "Founder");
  const lastFounder = isFounder && member.isActive && state.activeFounders <= 1;
  const deactivateBlock = member.isSelf ? t("members.cannotDeactivateSelf") : lastFounder ? t("members.lastFounder") : null;
  const founderLock = isFounder && (member.isSelf || lastFounder) ? { role: "Founder" as const, reason: member.isSelf ? t("members.cannotRemoveOwnFounder") : t("members.lastFounder") } : null;

  const saveProfile = async (event: FormEvent) => {
    event.preventDefault();
    if (!profile) {
      return;
    }

    const found = validateMemberForm({ fullName: profile.fullName, email: member.email, phone: profile.phone, roles: [] }, false);
    setProfileError(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setBusy(true);
    const outcome = await call(client.PUT("/api/admin/members/{userId}/profile", { params: { path: { userId } }, body: { fullName: profile.fullName.trim(), phone: profile.phone.trim() || null } }));
    setBusy(false);
    if (outcome.ok) {
      refresh(t("members.saved"));
    } else {
      setMessage({ error: true, text: failureText(outcome) });
    }
  };

  const saveRoles = async () => {
    if (!roles) {
      return;
    }

    if (roles.some((r) => r.role !== "Founder" && r.branchIds.length === 0)) {
      setMessage({ error: true, text: fieldText("branchRequired") });
      return;
    }

    setBusy(true);
    const outcome = await call(client.PUT("/api/admin/members/{userId}/roles", { params: { path: { userId } }, body: { roles } }));
    setBusy(false);
    if (outcome.ok) {
      refresh(t("members.saved"));
    } else {
      setMessage({ error: true, text: failureText(outcome) });
    }
  };

  const runConfirmed = async () => {
    setBusy(true);
    if (confirm === "reset") {
      const outcome = await call(client.POST("/api/admin/members/{userId}/reset-password", { params: { path: { userId } } }));
      setBusy(false);
      setConfirm(null);
      if (outcome.ok) {
        setTemporary(outcome.data.temporaryPassword);
      } else {
        setMessage({ error: true, text: failureText(outcome) });
      }

      return;
    }

    const outcome = await call(client.POST("/api/admin/members/{userId}/deactivate", { params: { path: { userId } } }));
    setBusy(false);
    setConfirm(null);
    if (outcome.ok) {
      refresh(t("members.saved"));
    } else {
      setMessage({ error: true, text: failureText(outcome) });
    }
  };

  const activate = async () => {
    setBusy(true);
    const outcome = await call(client.POST("/api/admin/members/{userId}/activate", { params: { path: { userId } } }));
    setBusy(false);
    if (outcome.ok) {
      refresh(t("members.saved"));
    } else {
      setMessage({ error: true, text: failureText(outcome) });
    }
  };

  return (
    <section className="card" aria-labelledby="member-title">
      <div className="actions">
        <button type="button" className="secondary" onClick={onBack}>{t("members.backToList")}</button>
      </div>
      <h3 id="member-title">
        {member.fullName} {!member.isActive && <span className="badge">{t("members.inactive")}</span>}
      </h3>
      <p className="notice">{member.email}</p>
      {message && <p className={message.error ? "form-error" : "notice"} role={message.error ? "alert" : "status"}>{message.text}</p>}

      <form className="auth-form" onSubmit={saveProfile} noValidate aria-label={t("members.profile")}>
        <label htmlFor="card-fullName">{t("members.fullName")}</label>
        <input id="card-fullName" value={profile?.fullName ?? ""} onChange={(e) => setProfile({ fullName: e.target.value, phone: profile?.phone ?? "" })}
          aria-invalid={profileError.fullName ? true : undefined} aria-describedby={profileError.fullName ? "card-fullName-error" : undefined} />
        {profileError.fullName && <p id="card-fullName-error" className="field-error" role="alert">{fieldText(profileError.fullName)}</p>}
        <label htmlFor="card-phone">{t("members.phone")}</label>
        <input id="card-phone" type="tel" value={profile?.phone ?? ""} onChange={(e) => setProfile({ fullName: profile?.fullName ?? "", phone: e.target.value })}
          aria-invalid={profileError.phone ? true : undefined} aria-describedby={profileError.phone ? "card-phone-error" : undefined} />
        {profileError.phone && <p id="card-phone-error" className="field-error" role="alert">{fieldText(profileError.phone)}</p>}
        <div className="actions">
          <button type="submit" disabled={busy} aria-busy={busy}>{t("members.save")}</button>
        </div>
      </form>

      <RolesEditor idPrefix="card" value={roles ?? []} branches={branches} onChange={setRoles} lockedRole={founderLock} />
      <div className="actions">
        <button type="button" disabled={busy} onClick={saveRoles}>{t("members.saveRoles")}</button>
      </div>

      <div className="actions">
        {member.isActive ? (
          <button type="button" className="secondary" disabled={deactivateBlock !== null || busy} aria-describedby={deactivateBlock ? "deactivate-reason" : undefined} onClick={() => setConfirm("deactivate")}>
            {t("members.deactivate")}
          </button>
        ) : (
          <button type="button" className="secondary" disabled={busy} onClick={activate}>{t("members.activate")}</button>
        )}
        {deactivateBlock && <span id="deactivate-reason" className="notice">{deactivateBlock}</span>}
        <button type="button" className="secondary" disabled={!member.canResetPassword || busy} aria-describedby={member.canResetPassword ? undefined : "reset-reason"} onClick={() => setConfirm("reset")}>
          {t("members.resetPassword")}
        </button>
        {!member.canResetPassword && <span id="reset-reason" className="notice">{t("members.resetBlocked")}</span>}
      </div>
      {confirm && (
        <ConfirmDialog
          message={confirm === "reset" ? t("members.confirmReset") : t("members.confirmDeactivate")}
          busy={busy}
          onCancel={() => setConfirm(null)}
          onConfirm={runConfirmed}
        />
      )}
    </section>
  );
}
