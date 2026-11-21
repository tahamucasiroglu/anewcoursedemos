import { useCallback, useState } from "react";
import {
  exportFileName,
  problemMessage,
  reportQuery,
  turkeyToday,
  validateReportFilter,
  type AttendanceReportPage,
  type AuthSession,
  type BalancesReportPage,
  type BranchSummary,
  type ClassroomSummary,
  type PaymentsReportPage,
  type ReportFilterErrors,
  type ReportFilterValues,
  type ReportKind,
} from "@okul/api-client";
import { defaultReportFilter } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call, type ApiOutcome } from "../admin/adminApi";
import { AttendanceReportTable } from "./AttendanceReportTable";
import { BalancesReportTable } from "./BalancesReportTable";
import { fill } from "./dashboardText";
import { Pager } from "./Pager";
import { PaymentsReportTable } from "./PaymentsReportTable";
import { ReportFilters } from "./ReportFilters";
import { saveFile } from "./saveFile";
import { useLoad } from "./useLoad";

type Client = AuthSession["client"];
type Report =
  | { kind: "attendance"; page: AttendanceReportPage }
  | { kind: "payments"; page: PaymentsReportPage }
  | { kind: "balances"; page: BalancesReportPage };
type Applied = { kind: ReportKind; values: ReportFilterValues; page: number };
type ExportState = { kind: "idle" } | { kind: "busy" } | { kind: "done"; name: string } | { kind: "error"; message: string };

const routeKinds: Record<string, ReportKind> = { devamsizlik: "attendance", tahsilat: "payments", borc: "balances" };
const filePrefix: Record<ReportKind, string> = { attendance: "devamsizlik-raporu", payments: "tahsilat-raporu", balances: "borc-gecikme-raporu" };

interface Props {
  client: Client;
  branches: BranchSummary[];
  route: string;
}

/**
 * Raporlar sekmesi (spec 0017 gereksinim 6–10, UX): rapor türü + süzgeçler → "Göster" (alan hatası varsa istek gitmez) → tablo + toplamlar +
 * 50'lik sayfa → "Excel'e aktar" (ekrandaki süzgeçle; hazırlanırken düğme kilitli, "Hazırlanıyor…"; iner ya da sunucunun hatası — ör. "Sonuç
 * çok büyük…" — duyurulur). Varsayılan aralık bu ayın ilk günü – bugün (Türkiye günü). "#/panel/raporlar?rapor=borc&amp;gecikenler=1" kartın
 * "Tümünü gör" bağlantısıdır. Durumlar: yükleniyor / "Bu süzgeçlere uyan kayıt yok" / dolu / hata + "Tekrar dene".
 */
export function ReportsTab({ client, branches, route }: Props) {
  const params = new URLSearchParams(route.split("?")[1] ?? "");
  const initialKind = routeKinds[params.get("rapor") ?? ""] ?? "attendance";
  const [kind, setKind] = useState<ReportKind>(initialKind);
  const [draft, setDraft] = useState<ReportFilterValues>(() => ({ ...defaultReportFilter(turkeyToday()), overdueOnly: params.get("gecikenler") === "1" }));
  const [errors, setErrors] = useState<ReportFilterErrors>({});
  const [applied, setApplied] = useState<Applied>(() => ({ kind: initialKind, values: draft, page: 1 }));
  const [exporting, setExporting] = useState<ExportState>({ kind: "idle" });

  const loadClassrooms = useCallback(() => call(client.GET("/api/classrooms")), [client]);
  const classrooms = useLoad<ClassroomSummary[]>(loadClassrooms, 0);
  const loadReport = useCallback(() => fetchReport(client, applied), [client, applied]);
  const { state, retry } = useLoad<Report>(loadReport, 0);

  const show = () => {
    const found = validateReportFilter(kind, draft);
    setErrors(found);
    if (Object.keys(found).length === 0) {
      setExporting({ kind: "idle" });
      setApplied({ kind, values: draft, page: 1 });
    }
  };

  const exportFile = async () => {
    setExporting({ kind: "busy" });
    const query = reportQuery(applied.kind, applied.values);
    const fallback = `${filePrefix[applied.kind]}-${turkeyToday().split("-").reverse().join(".")}.csv`;
    try {
      const path = `/api/dashboard/reports/${applied.kind}/export` as const;
      const { data, error, response } = await client.GET(path, { params: { query }, parseAs: "blob" });
      if (response.ok && data) {
        const name = exportFileName(response.headers.get("content-disposition"), fallback);
        saveFile(data as Blob, name);
        setExporting({ kind: "done", name });
      } else {
        setExporting({ kind: "error", message: problemMessage(error) ?? t("dashboard.loadError") });
      }
    } catch {
      setExporting({ kind: "error", message: t("admin.unreachable") });
    }
  };

  return (
    <div className="admin-section">
      <ReportFilters
        kind={kind}
        values={draft}
        errors={errors}
        branches={branches}
        classrooms={classrooms.state.kind === "ready" ? classrooms.state.data : []}
        onKind={(next) => {
          setKind(next);
          setErrors({});
        }}
        onChange={setDraft}
        onSubmit={show}
      />
      {state.kind === "loading" && <p className="notice" aria-busy="true">{t("dashboard.loading")}</p>}
      {state.kind === "error" && (
        <div>
          <p className="form-error" role="alert">{state.message}</p>
          <div className="actions">
            <button type="button" onClick={retry}>{t("dashboard.retry")}</button>
          </div>
        </div>
      )}
      {state.kind === "ready" && Number(state.data.page.totalCount) === 0 && <p>{t("dashboard.reports.empty")}</p>}
      {state.kind === "ready" && Number(state.data.page.totalCount) > 0 && (
        <>
          <div className="actions">
            <button type="button" disabled={exporting.kind === "busy"} aria-busy={exporting.kind === "busy" ? true : undefined} onClick={() => void exportFile()}>
              {t(exporting.kind === "busy" ? "dashboard.reports.exporting" : "dashboard.reports.export")}
            </button>
          </div>
          {exporting.kind === "done" && <p className="notice">{fill("dashboard.reports.exported", { name: exporting.name })}</p>}
          {exporting.kind === "error" && <p className="form-error" role="alert">{exporting.message}</p>}
          {state.data.kind === "attendance" && <AttendanceReportTable report={state.data.page} />}
          {state.data.kind === "payments" && <PaymentsReportTable report={state.data.page} />}
          {state.data.kind === "balances" && <BalancesReportTable report={state.data.page} />}
          <Pager
            page={Number(state.data.page.page)}
            pageSize={Number(state.data.page.pageSize)}
            totalCount={Number(state.data.page.totalCount)}
            onPage={(page) => setApplied({ ...applied, page })}
          />
        </>
      )}
    </div>
  );
}

async function fetchReport(client: Client, applied: Applied): Promise<ApiOutcome<Report>> {
  const query = { ...reportQuery(applied.kind, applied.values), page: applied.page };
  if (applied.kind === "attendance") {
    const outcome = await call(client.GET("/api/dashboard/reports/attendance", { params: { query } }));
    return outcome.ok ? { ok: true, data: { kind: "attendance", page: outcome.data } } : outcome;
  }

  if (applied.kind === "payments") {
    const outcome = await call(client.GET("/api/dashboard/reports/payments", { params: { query } }));
    return outcome.ok ? { ok: true, data: { kind: "payments", page: outcome.data } } : outcome;
  }

  const outcome = await call(client.GET("/api/dashboard/reports/balances", { params: { query } }));
  return outcome.ok ? { ok: true, data: { kind: "balances", page: outcome.data } } : outcome;
}
