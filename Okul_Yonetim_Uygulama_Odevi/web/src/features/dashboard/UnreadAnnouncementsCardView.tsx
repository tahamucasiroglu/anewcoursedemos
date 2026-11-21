import { useCallback } from "react";
import { formatTurkeyDateTime, type AuthSession, type UnreadAnnouncementsCard } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "../admin/adminApi";
import { CardFrame } from "./CardFrame";
import { fill } from "./dashboardText";
import { useLoad } from "./useLoad";

interface Props {
  client: AuthSession["client"];
  branchId: string;
  refresh: number;
}

/**
 * Okunmayan duyurular kartı (spec 0017 gereksinim 5): "Okunmayan duyuru yok" ya da "Son 7 günde N duyuruyu M veli okumadı", en çok
 * okunmayandan ilk 10 duyuru (başlık → duyuru ayrıntısı "#/iletisim/duyuru/&lt;id&gt;", yayın zamanı, yayınlayan, "X / Y okudu").
 */
export function UnreadAnnouncementsCardView({ client, branchId, refresh }: Props) {
  const load = useCallback(
    () => call(client.GET("/api/dashboard/summary/announcements", { params: { query: branchId ? { branchId } : {} } })),
    [client, branchId],
  );
  const { state, retry } = useLoad<UnreadAnnouncementsCard>(load, refresh);
  return (
    <CardFrame id="card-unread" title={t("dashboard.unread.title")} state={state} onRetry={retry}>
      {(card) =>
        Number(card.announcementCount) === 0 ? (
          <p>{t("dashboard.unread.none")}</p>
        ) : (
          <>
            <p className="summary-figure">{fill("dashboard.unread.summary", { n: card.announcementCount, guardians: card.unreadGuardianCount })}</p>
            <ul className="summary-list">
              {card.top.map((row) => (
                <li key={row.id}>
                  <a href={`#/iletisim/duyuru/${row.id}`}>{row.title}</a>
                  <span className="badge"> {fill("dashboard.unread.by", { date: formatTurkeyDateTime(row.publishedAt), author: row.authorName })}</span>
                  <span> — {fill("dashboard.unread.read", { read: row.readCount, total: row.recipientCount })}</span>
                </li>
              ))}
            </ul>
          </>
        )
      }
    </CardFrame>
  );
}
