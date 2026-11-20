import { useEffect, useState } from "react";
import {
  callApi,
  initialMarks,
  isStaleSave,
  saveBody,
  saveResultText,
  setMark,
  type AttendanceMarks,
  type AttendanceSheetView,
  type AttendanceStatus,
  type AuthSession,
  type MyClassroomRow,
} from "@okul/api-client";
import { t } from "../../i18n/t";

type Client = AuthSession["client"];
export type ClassroomsState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; rows: MyClassroomRow[] };
export type SheetState =
  | { kind: "loading" }
  | { kind: "error"; message: string; forbidden: boolean }
  | { kind: "ready"; sheet: AttendanceSheetView; marks: AttendanceMarks; saving: boolean; result: string | null; saveError: string | null };

function failure(outcome: { unreachable: true } | { unreachable: false; message: string | null }): string {
  return outcome.unreachable ? t("attendance.unreachable") : outcome.message ?? t("attendance.loadError");
}

/** Öğretmenin "Sınıflarım" listesi (spec 0010): yükleniyor → dolu/boş ya da hata; <c>retry</c> yeniden ister. */
export function useMyClassrooms(client: Client) {
  const [state, setState] = useState<ClassroomsState>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/attendance/classrooms")).then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", rows: outcome.data } : { kind: "error", message: failure(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  const retry = () => {
    setState({ kind: "loading" });
    setReload((n) => n + 1);
  };

  return { state, retry };
}

/**
 * Bir sınıfın bugünkü yoklaması (spec 0010): liste (kayıtlı durumlar ya da herkes "Var"), dokunuşla durum değişimi ve tek
 * seferde kaydetme. Kaydederken düğme kilitli; hata olursa işaretler korunur (tekrar dene = yeniden Kaydet). Erişim yoksa
 * (403/404) "Bu sayfaya erişiminiz yok.". Ekran açıldıktan sonra başkası kaydettiyse (409) liste yeniden okunur ve sunucunun
 * mesajı gösterilir (0010 review F-1).
 */
export function useAttendanceSheet(client: Client, classroomId: string) {
  const [state, setState] = useState<SheetState>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/attendance/classrooms/{id}/today", { params: { path: { id: classroomId } } })).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setState({ kind: "ready", sheet: outcome.data, marks: initialMarks(outcome.data.students), saving: false, result: null, saveError: null });
      } else {
        const forbidden = !outcome.unreachable && (outcome.status === 403 || outcome.status === 404);
        setState({ kind: "error", message: forbidden ? t("attendance.forbidden") : failure(outcome), forbidden });
      }
    });
    return () => {
      active = false;
    };
  }, [client, classroomId, reload]);

  const retry = () => {
    setState({ kind: "loading" });
    setReload((n) => n + 1);
  };

  const mark = (studentId: string, status: AttendanceStatus) =>
    setState((s) => (s.kind === "ready" && !s.saving ? { ...s, marks: setMark(s.marks, studentId, status), result: null } : s));

  const save = async () => {
    if (state.kind !== "ready" || state.saving) {
      return;
    }

    const { sheet, marks } = state;
    setState({ ...state, saving: true, saveError: null, result: null });
    const outcome = await callApi(
      client.PUT("/api/attendance/classrooms/{id}/today", { params: { path: { id: classroomId } }, body: saveBody(sheet.day, marks, sheet.version) }),
    );
    if (isStaleSave(outcome)) {
      const notice = (!outcome.ok && !outcome.unreachable && outcome.message) || t("attendance.stale");
      const fresh = await callApi(client.GET("/api/attendance/classrooms/{id}/today", { params: { path: { id: classroomId } } }));
      setState(
        fresh.ok
          ? { kind: "ready", sheet: fresh.data, marks: initialMarks(fresh.data.students), saving: false, result: null, saveError: notice }
          : { kind: "error", message: failure(fresh), forbidden: false },
      );
      return;
    }

    setState((s) => {
      if (s.kind !== "ready") {
        return s;
      }

      if (outcome.ok) {
        const result = saveResultText(outcome.data, { saved: t("attendance.result"), withoutGuardian: t("attendance.withoutGuardian") });
        return { ...s, saving: false, result, sheet: { ...s.sheet, isSaved: true, savedTime: outcome.data.savedTime, version: outcome.data.version } };
      }

      const message = outcome.unreachable ? t("attendance.unreachable") : outcome.message ?? t("attendance.saveError");
      return { ...s, saving: false, saveError: message };
    });
  };

  return { state, retry, mark, save };
}
