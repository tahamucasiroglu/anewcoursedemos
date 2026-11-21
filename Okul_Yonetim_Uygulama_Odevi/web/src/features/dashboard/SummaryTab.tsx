import { useState } from "react";
import { dayText, turkeyToday, type AuthSession, type BranchSummary } from "@okul/api-client";
import { t } from "../../i18n/t";
import { AttendanceCardView } from "./AttendanceCardView";
import { fill } from "./dashboardText";
import { OverdueCardView } from "./OverdueCardView";
import { UnreadAnnouncementsCardView } from "./UnreadAnnouncementsCardView";

interface Props {
  client: AuthSession["client"];
  branches: BranchSummary[];
}

/**
 * Günlük özet (spec 0017 gereksinim 2–5, UX): üstte şube süzgeci ("Bütün şubeler" ya da tek etkin şube), "Yenile" ve bugünün tarihi (Türkiye
 * günü); altında üç kart — her biri kendi isteğini yapar, kendi yükleniyor/hata/tekrar dene durumunu çizer. Canlı güncelleme yok.
 */
export function SummaryTab({ client, branches }: Props) {
  const [branchId, setBranchId] = useState("");
  const [refresh, setRefresh] = useState(0);
  return (
    <div className="admin-section">
      <div className="summary-bar">
        <label htmlFor="summary-branch">{t("dashboard.branch")}</label>
        <select id="summary-branch" value={branchId} onChange={(e) => setBranchId(e.target.value)}>
          <option value="">{t("dashboard.allBranches")}</option>
          {branches.map((b) => (
            <option key={b.id} value={b.id}>{b.name}</option>
          ))}
        </select>
        <button type="button" onClick={() => setRefresh((n) => n + 1)}>{t("dashboard.refresh")}</button>
        <span className="notice">{fill("dashboard.today", { date: dayText(turkeyToday()) })}</span>
      </div>
      <div className="summary-cards">
        <AttendanceCardView client={client} branchId={branchId} refresh={refresh} />
        <OverdueCardView client={client} branchId={branchId} refresh={refresh} />
        <UnreadAnnouncementsCardView client={client} branchId={branchId} refresh={refresh} />
      </div>
    </div>
  );
}
