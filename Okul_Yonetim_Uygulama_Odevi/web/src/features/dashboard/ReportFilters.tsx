import type { FormEvent } from "react";
import type { BranchSummary, ClassroomSummary, ReportFilterErrors, ReportFilterValues, ReportKind } from "@okul/api-client";
import { REPORT_KINDS } from "@okul/api-client";
import { t } from "../../i18n/t";
import { reportFieldText } from "./dashboardText";

interface Props {
  kind: ReportKind;
  values: ReportFilterValues;
  errors: ReportFilterErrors;
  branches: BranchSummary[];
  classrooms: ClassroomSummary[];
  onKind: (kind: ReportKind) => void;
  onChange: (values: ReportFilterValues) => void;
  onSubmit: () => void;
}

/**
 * Rapor seçimi ve süzgeçleri (spec 0017 gereksinim 6–9): rapor türü, başlangıç–bitiş (devamsızlık ve tahsilat), şube, sınıf ve "en az N gün
 * yok" (devamsızlık), "yalnızca gecikenler" (borç). Denetimli form — değerler üst bileşende; alan hataları alanın altında, alana bağlı
 * (aria-describedby, aria-invalid); hata varken istek gönderilmez (üst bileşen doğrular). Yalnızca klavyeyle kullanılır.
 */
export function ReportFilters({ kind, values, errors, branches, classrooms, onKind, onChange, onSubmit }: Props) {
  const submit = (event: FormEvent) => {
    event.preventDefault();
    onSubmit();
  };
  const set = (patch: Partial<ReportFilterValues>) => onChange({ ...values, ...patch });
  const visibleClassrooms = values.branchId ? classrooms.filter((c) => c.branchId === values.branchId) : classrooms;
  return (
    <form className="filters" onSubmit={submit} aria-label={t("dashboard.reports.filters")} noValidate>
      <label htmlFor="report-kind">{t("dashboard.reports.kind")}</label>
      <select id="report-kind" value={kind} onChange={(e) => onKind(e.target.value as ReportKind)}>
        {REPORT_KINDS.map((k) => (
          <option key={k} value={k}>{t(`dashboard.reports.${k}`)}</option>
        ))}
      </select>
      {kind !== "balances" && (
        <>
          <label htmlFor="report-start">{t("dashboard.reports.startDate")}</label>
          <div>
            <input
              id="report-start"
              type="date"
              value={values.startDate}
              aria-invalid={errors.startDate ? true : undefined}
              aria-describedby={errors.startDate ? "report-start-error" : undefined}
              onChange={(e) => set({ startDate: e.target.value })}
            />
            {errors.startDate && <p id="report-start-error" className="field-error">{reportFieldText(errors.startDate, "startDate")}</p>}
          </div>
          <label htmlFor="report-end">{t("dashboard.reports.endDate")}</label>
          <div>
            <input
              id="report-end"
              type="date"
              value={values.endDate}
              aria-invalid={errors.endDate ? true : undefined}
              aria-describedby={errors.endDate ? "report-end-error" : undefined}
              onChange={(e) => set({ endDate: e.target.value })}
            />
            {errors.endDate && <p id="report-end-error" className="field-error">{reportFieldText(errors.endDate, "endDate")}</p>}
          </div>
        </>
      )}
      <label htmlFor="report-branch">{t("dashboard.branch")}</label>
      <select id="report-branch" value={values.branchId} onChange={(e) => set({ branchId: e.target.value, classroomId: "" })}>
        <option value="">{t("dashboard.allBranches")}</option>
        {branches.map((b) => (
          <option key={b.id} value={b.id}>{b.name}</option>
        ))}
      </select>
      {kind === "attendance" && (
        <>
          <label htmlFor="report-classroom">{t("dashboard.reports.classroom")}</label>
          <select id="report-classroom" value={values.classroomId} onChange={(e) => set({ classroomId: e.target.value })}>
            <option value="">{t("dashboard.reports.allClassrooms")}</option>
            {visibleClassrooms.map((c) => (
              <option key={c.id} value={c.id}>{`${c.branchName} / ${c.name}`}</option>
            ))}
          </select>
          <label htmlFor="report-min-absent">{t("dashboard.reports.minAbsent")}</label>
          <div>
            <input
              id="report-min-absent"
              inputMode="numeric"
              value={values.minAbsent}
              aria-invalid={errors.minAbsent ? true : undefined}
              aria-describedby={errors.minAbsent ? "report-min-absent-error" : undefined}
              onChange={(e) => set({ minAbsent: e.target.value })}
            />
            {errors.minAbsent && <p id="report-min-absent-error" className="field-error">{reportFieldText(errors.minAbsent, "minAbsent")}</p>}
          </div>
        </>
      )}
      {kind === "balances" && (
        <>
          <span />
          <label className="check">
            <input type="checkbox" checked={values.overdueOnly} onChange={(e) => set({ overdueOnly: e.target.checked })} /> {t("dashboard.reports.overdueOnly")}
          </label>
        </>
      )}
      <span />
      <div className="actions">
        <button type="submit">{t("dashboard.reports.show")}</button>
      </div>
    </form>
  );
}
