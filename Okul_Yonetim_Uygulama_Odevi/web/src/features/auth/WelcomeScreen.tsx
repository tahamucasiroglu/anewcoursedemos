import { noticesLinkText, type Me } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

interface Props {
  me: Me;
  onSwitchSchool: () => void;
  onLogout: () => void;
}

/**
 * Hoş geldin ekranı (spec 0004): ad, seçili okul, o okuldaki roller ve görülebilen şubeler; şube yoksa açık mesaj.
 * "Okul değiştir" yalnızca birden çok okul varsa görünür; "Çıkış" oturumu kapatır. "Platform paneli" yalnızca platform
 * yöneticisinde (spec 0007). "Okul takvimi" seçili okulu olan herkese (spec 0008). "Öğrenciler" yalnızca kurucuya (spec 0009). "Yoklama al" yalnızca
 * öğretmene, okunmamış sayılı "Bildirimler (n)" yalnızca veliye (spec 0010). "İletişim" yalnızca kurucu ve öğretmene (spec 0011).
 */
export function WelcomeScreen({ me, onSwitchSchool, onLogout }: Props) {
  return (
    <section className="card" aria-labelledby="welcome-title">
      <h2 id="welcome-title">
        {t("welcome.greeting")} {me.fullName}
      </h2>
      <dl className="facts">
        <dt>{t("welcome.school")}</dt>
        <dd>{me.tenant?.name}</dd>
        <dt>{t("welcome.roles")}</dt>
        <dd>{me.roles.map((role) => roleLabel(role)).join(", ")}</dd>
        <dt>{t("welcome.branches")}</dt>
        <dd>
          {me.branches.length === 0 ? (
            t("welcome.noBranches")
          ) : (
            <ul className="branch-list">
              {me.branches.map((branch) => (
                <li key={branch.id}>{branch.name}</li>
              ))}
            </ul>
          )}
        </dd>
      </dl>
      <div className="actions">
        {me.roles.includes("Teacher") && (
          <a className="button-link" href="#/yoklama">
            {t("attendance.link")}
          </a>
        )}
        {(me.roles.includes("Founder") || me.roles.includes("Teacher")) && (
          <a className="button-link" href="#/iletisim">
            {t("communication.link")}
          </a>
        )}
        {me.roles.includes("Guardian") && (
          <a className="button-link" href="#/bildirimler">
            {noticesLinkText(me.unreadNoticeCount, t("notices.link"), t("notices.linkCount"))}
          </a>
        )}
        <a className="button-link" href="#/hesabim">
          {t("account.link")}
        </a>
        {me.tenant && (
          <a className="button-link" href="#/takvim">
            {t("calendar.link")}
          </a>
        )}
        {me.isPlatformAdmin && (
          <a className="button-link" href="#/platform">
            {t("platform.link")}
          </a>
        )}
        {me.roles.includes("Founder") && (
          <a className="button-link" href="#/ogrenciler">
            {t("students.link")}
          </a>
        )}
        {me.roles.includes("Founder") && (
          <a className="button-link" href="#/yonetim">
            {t("admin.link")}
          </a>
        )}
        {me.tenants.length > 1 && (
          <button type="button" className="secondary" onClick={onSwitchSchool}>
            {t("welcome.switchSchool")}
          </button>
        )}
        <button type="button" onClick={onLogout}>
          {t("welcome.logout")}
        </button>
      </div>
    </section>
  );
}

function roleLabel(role: string): string {
  const key = `role.${role}` as TextKey;
  return key in roleKeys ? t(key) : role;
}

const roleKeys: Record<string, true> = { "role.Founder": true, "role.Teacher": true, "role.Guardian": true, "role.Accounting": true };
