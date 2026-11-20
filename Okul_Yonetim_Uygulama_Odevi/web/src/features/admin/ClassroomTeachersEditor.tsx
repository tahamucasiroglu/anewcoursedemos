import { useEffect, useState } from "react";
import { fillText, type AuthSession, type ClassroomTeachersView, type TeacherOption } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "./adminApi";
import { failureText } from "./adminText";

type State =
  | { kind: "loading" }
  | { kind: "error"; message: string }
  | { kind: "ready"; view: ClassroomTeachersView; assigned: TeacherOption[]; candidates: TeacherOption[]; saving: boolean; message: string | null; error: string | null };

interface Props {
  client: AuthSession["client"];
  classroomId: string;
  onClose: () => void;
}

function byName(a: TeacherOption, b: TeacherOption) {
  return a.fullName.localeCompare(b.fullName, "tr");
}

/**
 * Sınıf öğretmenleri (spec 0010 gereksinim 1, kurucu, Sınıflar sekmesindeki satırdan açılır): atanmış öğretmenler
 * ("Kaldır") ve sınıfın şubesindeki etkin öğretmenler ("Ekle"); "Kaydet" listeyi tek seferde gönderir. Durumlar:
 * yükleniyor, şubede öğretmen yok ("Bu şubede öğretmen yok"), kaydediliyor (düğme kilitli), kaydedildi, hata (sunucu
 * mesajı; hiçbir şey değişmez). Çağıran, sınıfa bağlı <c>key</c> verir (T15).
 */
export function ClassroomTeachersEditor({ client, classroomId, onClose }: Props) {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    call(client.GET("/api/admin/classrooms/{id}/teachers", { params: { path: { id: classroomId } } })).then((outcome) => {
      if (!active) {
        return;
      }

      setState(outcome.ok
        ? { kind: "ready", view: outcome.data, assigned: outcome.data.assigned, candidates: outcome.data.candidates, saving: false, message: null, error: null }
        : { kind: "error", message: failureText(outcome) });
    });
    return () => {
      active = false;
    };
  }, [client, classroomId, reload]);

  if (state.kind === "loading") {
    return <p className="notice" aria-busy="true">{t("classroomTeachers.loading")}</p>;
  }

  if (state.kind === "error") {
    return (
      <div className="card">
        <p className="form-error" role="alert">{state.message}</p>
        <div className="actions">
          <button type="button" onClick={() => { setState({ kind: "loading" }); setReload((n) => n + 1); }}>{t("admin.retry")}</button>
          <button type="button" className="secondary" onClick={onClose}>{t("classroomTeachers.close")}</button>
        </div>
      </div>
    );
  }

  const { view, assigned, candidates, saving, message, error } = state;
  const move = (teacher: TeacherOption, add: boolean) =>
    setState({
      ...state,
      message: null,
      error: null,
      assigned: add ? [...assigned, teacher].sort(byName) : assigned.filter((a) => a.userId !== teacher.userId),
      candidates: add ? candidates.filter((c) => c.userId !== teacher.userId) : [...candidates, teacher].sort(byName),
    });

  const save = async () => {
    setState({ ...state, saving: true, message: null, error: null });
    const outcome = await call(client.PUT("/api/admin/classrooms/{id}/teachers", {
      params: { path: { id: classroomId } },
      body: { teacherUserIds: assigned.map((a) => a.userId) },
    }));
    setState((s) => {
      if (s.kind !== "ready") {
        return s;
      }

      return outcome.ok
        ? { ...s, view: outcome.data, assigned: outcome.data.assigned, candidates: outcome.data.candidates, saving: false, message: t("classroomTeachers.saved") }
        : { ...s, saving: false, error: failureText(outcome) };
    });
  };

  const titleId = `classroom-teachers-${classroomId}`;
  return (
    <section className="card" aria-labelledby={titleId}>
      <h3 id={titleId}>{fillText(t("classroomTeachers.title"), { name: `${view.branchName} ${view.classroomName}` })}</h3>
      {assigned.length === 0 && candidates.length === 0 ? (
        <p>{t("classroomTeachers.noTeachers")}</p>
      ) : (
        <>
          <h4>{t("classroomTeachers.assigned")}</h4>
          {assigned.length === 0 ? (
            <p>{t("classroomTeachers.noneAssigned")}</p>
          ) : (
            <ul className="admin-list">
              {assigned.map((teacher) => (
                <li key={teacher.userId} className="admin-row">
                  <span>{teacher.fullName} <span className="notice">· {teacher.email}</span></span>
                  <button type="button" className="secondary" disabled={saving} aria-label={`${teacher.fullName}: ${t("classroomTeachers.remove")}`} onClick={() => move(teacher, false)}>
                    {t("classroomTeachers.remove")}
                  </button>
                </li>
              ))}
            </ul>
          )}
          <h4>{t("classroomTeachers.candidates")}</h4>
          {candidates.length === 0 ? (
            <p>{t("classroomTeachers.noTeachers")}</p>
          ) : (
            <ul className="admin-list">
              {candidates.map((teacher) => (
                <li key={teacher.userId} className="admin-row">
                  <span>{teacher.fullName} <span className="notice">· {teacher.email}</span></span>
                  <button type="button" className="secondary" disabled={saving} aria-label={`${teacher.fullName}: ${t("classroomTeachers.add")}`} onClick={() => move(teacher, true)}>
                    {t("classroomTeachers.add")}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
      {message && <p role="status">{message}</p>}
      {error && <p className="form-error" role="alert">{error}</p>}
      <div className="actions">
        <button type="button" disabled={saving} aria-busy={saving} onClick={() => void save()}>
          {saving ? t("classroomTeachers.saving") : t("classroomTeachers.save")}
        </button>
        <button type="button" className="secondary" onClick={onClose}>{t("classroomTeachers.close")}</button>
      </div>
    </section>
  );
}
