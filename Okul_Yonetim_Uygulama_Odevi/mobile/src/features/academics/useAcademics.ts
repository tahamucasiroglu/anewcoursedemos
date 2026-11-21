import { useEffect, useState } from "react";
import {
  callApi,
  countSubmissions,
  gradesBody,
  gradesResultText,
  initialScores,
  initialSubmissionMarks,
  invalidScores,
  submissionsBody,
  submissionsResultText,
  type ApiOutcome,
  type AssessmentGradesView,
  type AssessmentRow,
  type AssignmentRow,
  type AssignmentSubmissionsView,
  type AuthSession,
  type ChildAcademicsView,
  type SubmissionMarks,
  type SubmissionStatus,
} from "@okul/api-client";
import { t } from "../../i18n/t";

type Client = AuthSession["client"];
type Failure = Exclude<ApiOutcome<unknown>, { ok: true }>;
export type ListState<T> = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; rows: T[] };
export type SubmissionsState =
  | { kind: "loading" }
  | { kind: "error"; message: string; forbidden: boolean }
  | { kind: "ready"; view: AssignmentSubmissionsView; marks: SubmissionMarks; saving: boolean; result: string | null; saveError: string | null };
export type GradesState =
  | { kind: "loading" }
  | { kind: "error"; message: string; forbidden: boolean }
  | { kind: "ready"; view: AssessmentGradesView; scores: Record<string, string>; invalid: string[]; saving: boolean; result: string | null; saveError: string | null };
export type ChildAcademicsState = { kind: "loading" } | { kind: "gone" } | { kind: "error"; message: string } | { kind: "ready"; view: ChildAcademicsView };

function failure(outcome: Failure, fallback: string): string {
  return outcome.unreachable ? t("academics.unreachable") : outcome.message ?? fallback;
}

function isForbidden(outcome: Failure): boolean {
  return !outcome.unreachable && (outcome.status === 403 || outcome.status === 404);
}

/** Sınıfın ödevlerini ya da değerlendirmelerini çağırır. */
function loadList(client: Client, kind: "assignments" | "assessments", classroomId: string) {
  const params = { params: { path: { classroomId } } };
  return kind === "assignments"
    ? callApi(client.GET("/api/academics/classrooms/{classroomId}/assignments", params))
    : callApi(client.GET("/api/academics/classrooms/{classroomId}/assessments", params));
}

/** Bir listeyi yükler: yükleniyor → dolu/boş ya da hata; <c>retry</c> yeniden ister. */
function useList<T>(client: Client, kind: "assignments" | "assessments", classroomId: string) {
  const [state, setState] = useState<ListState<T>>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    void loadList(client, kind, classroomId).then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", rows: outcome.data as T[] } : { kind: "error", message: failure(outcome, t("academics.loadError")) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, kind, classroomId, reload]);

  const retry = () => {
    setState({ kind: "loading" });
    setReload((n) => n + 1);
  };

  return { state, retry };
}

/** Sınıfın ödevleri (spec 0013 gereksinim 3). */
export function useAssignments(client: Client, classroomId: string) {
  return useList<AssignmentRow>(client, "assignments", classroomId);
}

/** Sınıfın değerlendirmeleri (spec 0013 gereksinim 6). */
export function useAssessments(client: Client, classroomId: string) {
  return useList<AssessmentRow>(client, "assessments", classroomId);
}

/**
 * Ödevin teslim listesi (spec 0013 gereksinim 4): dokunuşla durum ve tek seferde kaydetme. Kaydederken düğme kilitli; hata olursa
 * işaretler korunur (tekrar Kaydet). Erişim yoksa (403/404) "Bu sayfaya erişiminiz yok.".
 */
export function useSubmissions(client: Client, assignmentId: string) {
  const [state, setState] = useState<SubmissionsState>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    void callApi(client.GET("/api/academics/assignments/{id}/submissions", { params: { path: { id: assignmentId } } })).then((outcome) => {
      if (!active) {
        return;
      }

      setState(
        outcome.ok
          ? { kind: "ready", view: outcome.data, marks: initialSubmissionMarks(outcome.data.students), saving: false, result: null, saveError: null }
          : { kind: "error", message: isForbidden(outcome) ? t("academics.forbidden") : failure(outcome, t("academics.loadError")), forbidden: isForbidden(outcome) },
      );
    });
    return () => {
      active = false;
    };
  }, [client, assignmentId, reload]);

  const retry = () => {
    setState({ kind: "loading" });
    setReload((n) => n + 1);
  };

  const mark = (studentId: string, status: SubmissionStatus) =>
    setState((s) => (s.kind === "ready" && !s.saving ? { ...s, marks: { ...s.marks, [studentId]: status }, result: null } : s));

  const save = async () => {
    if (state.kind !== "ready" || state.saving) {
      return;
    }

    const { marks } = state;
    setState({ ...state, saving: true, saveError: null, result: null });
    const outcome = await callApi(client.PUT("/api/academics/assignments/{id}/submissions", { params: { path: { id: assignmentId } }, body: submissionsBody(marks) }));
    setState((s) => {
      if (s.kind !== "ready") {
        return s;
      }

      return outcome.ok
        ? { ...s, saving: false, result: submissionsResultText(outcome.data, t("academics.submissionsResult")) }
        : { ...s, saving: false, saveError: failure(outcome, t("academics.saveError")) };
    });
  };

  return { state, retry, mark, save, counts: state.kind === "ready" ? countSubmissions(state.marks) : null };
}

/**
 * Değerlendirmenin puan listesi (spec 0013 gereksinim 6): puan alanları ve tek seferde kaydetme. Geçersiz puan (0–100 dışı,
 * ondalık, harf) istek gitmeden öğrencinin alanında gösterilir; sunucu hatasında puanlar korunur.
 */
export function useGrades(client: Client, assessmentId: string) {
  const [state, setState] = useState<GradesState>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    void callApi(client.GET("/api/academics/assessments/{id}/grades", { params: { path: { id: assessmentId } } })).then((outcome) => {
      if (!active) {
        return;
      }

      setState(
        outcome.ok
          ? { kind: "ready", view: outcome.data, scores: initialScores(outcome.data.students), invalid: [], saving: false, result: null, saveError: null }
          : { kind: "error", message: isForbidden(outcome) ? t("academics.forbidden") : failure(outcome, t("academics.loadError")), forbidden: isForbidden(outcome) },
      );
    });
    return () => {
      active = false;
    };
  }, [client, assessmentId, reload]);

  const retry = () => {
    setState({ kind: "loading" });
    setReload((n) => n + 1);
  };

  const setScore = (studentId: string, text: string) =>
    setState((s) => (s.kind === "ready" && !s.saving
      ? { ...s, scores: { ...s.scores, [studentId]: text }, invalid: s.invalid.filter((id) => id !== studentId), result: null }
      : s));

  const save = async () => {
    if (state.kind !== "ready" || state.saving) {
      return;
    }

    const invalid = invalidScores(state.scores);
    if (invalid.length > 0) {
      setState({ ...state, invalid, result: null, saveError: t("academics.scoresInvalid") });
      return;
    }

    const { scores } = state;
    setState({ ...state, saving: true, saveError: null, result: null });
    const outcome = await callApi(client.PUT("/api/academics/assessments/{id}/grades", { params: { path: { id: assessmentId } }, body: gradesBody(scores) }));
    setState((s) => {
      if (s.kind !== "ready") {
        return s;
      }

      return outcome.ok
        ? { ...s, saving: false, result: gradesResultText(outcome.data, t("academics.gradesResult")) }
        : { ...s, saving: false, saveError: failure(outcome, t("academics.saveError")) };
    });
  };

  return { state, retry, setScore, save };
}

/** Velinin çocuk özeti (spec 0013 gereksinim 7): bağ kalktıysa ya da öğrenci ayrıldıysa (404) "artık görüntülenemiyor". */
export function useChildAcademics(client: Client, studentId: string) {
  const [state, setState] = useState<ChildAcademicsState>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    void callApi(client.GET("/api/my/children/{studentId}/academics", { params: { path: { studentId } } })).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setState({ kind: "ready", view: outcome.data });
      } else if (!outcome.unreachable && outcome.status === 404) {
        setState({ kind: "gone" });
      } else {
        setState({ kind: "error", message: failure(outcome, t("childAcademics.loadError")) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, studentId, reload]);

  const retry = () => {
    setState({ kind: "loading" });
    setReload((n) => n + 1);
  };

  return { state, retry };
}
