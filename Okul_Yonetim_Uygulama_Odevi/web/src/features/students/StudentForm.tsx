import { useEffect, useRef, useState, type FormEvent } from "react";
import {
  validateStudentForm,
  type AuthSession,
  type BranchAdminView,
  type ClassroomSummary,
  type StudentDetail,
  type StudentFormErrors,
  type StudentFormValues,
} from "@okul/api-client";
import { t } from "../../i18n/t";
import { failureText } from "../admin/adminText";
import { call, turkeyToday } from "./studentsApi";
import { studentFieldText } from "./studentsText";

type Client = AuthSession["client"];

interface Props {
  client: Client;
  /** Verilirse düzenleme: şube kilitli, sağlık alanları yok (kartın Sağlık bölümünde). */
  student?: StudentDetail;
  onCancel: () => void;
  onSaved: (id: string) => void;
}

function initial(student?: StudentDetail): StudentFormValues {
  return {
    fullName: student?.fullName ?? "",
    birthDate: student?.birthDate ?? "",
    schoolNumber: student?.schoolNumber ?? "",
    branchId: student?.branchId ?? "",
    classroomId: student?.classroomId ?? "",
    healthNote: "",
    allergy: "",
  };
}

/**
 * Öğrenci ekle / düzenle formu (spec 0009 gereksinim 3): ad-soyad, doğum tarihi, okul numarası, şube (yalnızca etkin
 * şubeler; düzenlemede kilitli — nakil yok), sınıf (seçilen şubenin etkin sınıfları ya da "Sınıf atanmamış"); eklemede
 * sağlık notu ve alerji. Alan hataları istek göndermeden gösterilir ve alanla ilişkilidir; gönderilirken düğme kilitli;
 * sunucu hatası (ör. "Bu okul numarası başka bir öğrencide") duyurulur. Açılınca odak ilk alana gelir. Düzenlenen kayda bağlı anahtarla kurulmalı (T15).
 */
export function StudentForm({ client, student, onCancel, onSaved }: Props) {
  const [values, setValues] = useState(() => initial(student));
  const [errors, setErrors] = useState<StudentFormErrors>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);
  const [branches, setBranches] = useState<BranchAdminView[]>([]);
  const [classrooms, setClassrooms] = useState<ClassroomSummary[]>([]);
  const firstField = useRef<HTMLInputElement>(null);

  // Form açılınca odak ilk alana gelir (yalnızca klavyeyle kullanım).
  useEffect(() => firstField.current?.focus(), []);

  useEffect(() => {
    let active = true;
    Promise.all([call(client.GET("/api/admin/branches")), call(client.GET("/api/classrooms"))]).then(([b, c]) => {
      if (active) {
        setBranches(b.ok ? b.data : []);
        setClassrooms(c.ok ? c.data : []);
      }
    });
    return () => {
      active = false;
    };
  }, [client]);

  const editing = student !== undefined;
  const branchOptions = branches.filter((b) => b.isActive || b.id === values.branchId);
  const classroomOptions = classrooms.filter((c) => c.branchId === values.branchId && c.isActive);
  const keepsOldClassroom = editing && student.classroomId !== null && !classroomOptions.some((c) => c.id === student.classroomId);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateStudentForm(values, turkeyToday());
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    setServerError(null);
    const body = {
      fullName: values.fullName.trim(),
      birthDate: values.birthDate,
      schoolNumber: values.schoolNumber.trim() || null,
      branchId: values.branchId,
      classroomId: values.classroomId || null,
      healthNote: editing ? null : values.healthNote.trim() || null,
      allergy: editing ? null : values.allergy.trim() || null,
    };
    const outcome = editing
      ? await call(client.PUT("/api/admin/students/{id}", { params: { path: { id: student.id } }, body }))
      : await call(client.POST("/api/admin/students", { body }));
    setSaving(false);
    if (outcome.ok) {
      onSaved(outcome.data.id);
    } else {
      setServerError(failureText(outcome));
    }
  };

  const described = (name: keyof StudentFormValues, extra?: string) => {
    const ids = [errors[name] ? `student-${name}-error` : undefined, extra].filter(Boolean).join(" ");
    return ids === "" ? undefined : ids;
  };

  const error = (name: keyof StudentFormValues) => {
    const found = errors[name];
    return found ? <p id={`student-${name}-error`} className="field-error" role="alert">{studentFieldText(found)}</p> : null;
  };

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={editing ? t("students.edit") : t("students.add")}>
      <label htmlFor="student-fullName">{t("students.fullName")}</label>
      <input id="student-fullName" ref={firstField} value={values.fullName} onChange={(e) => setValues({ ...values, fullName: e.target.value })}
        aria-invalid={errors.fullName ? true : undefined} aria-describedby={described("fullName")} />
      {error("fullName")}

      <label htmlFor="student-birthDate">{t("students.birthDate")}</label>
      <input id="student-birthDate" type="date" value={values.birthDate} onChange={(e) => setValues({ ...values, birthDate: e.target.value })}
        aria-invalid={errors.birthDate ? true : undefined} aria-describedby={described("birthDate")} />
      {error("birthDate")}

      <label htmlFor="student-schoolNumber">{t("students.schoolNumber")}</label>
      <input id="student-schoolNumber" value={values.schoolNumber} onChange={(e) => setValues({ ...values, schoolNumber: e.target.value })}
        aria-invalid={errors.schoolNumber ? true : undefined} aria-describedby={described("schoolNumber")} />
      {error("schoolNumber")}

      <label htmlFor="student-branchId">{t("students.branch")}</label>
      <select id="student-branchId" value={values.branchId} disabled={editing}
        onChange={(e) => setValues({ ...values, branchId: e.target.value, classroomId: "" })}
        aria-invalid={errors.branchId ? true : undefined} aria-describedby={described("branchId", editing ? "student-branch-fixed" : undefined)}>
        <option value="">{t("students.chooseBranch")}</option>
        {branchOptions.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
        {editing && !branchOptions.some((b) => b.id === student.branchId) && <option value={student.branchId}>{student.branchName}</option>}
      </select>
      {editing && <p id="student-branch-fixed" className="notice">{t("students.branchFixed")}</p>}
      {error("branchId")}

      <label htmlFor="student-classroomId">{t("students.classroom")}</label>
      <select id="student-classroomId" value={values.classroomId} onChange={(e) => setValues({ ...values, classroomId: e.target.value })}>
        <option value="">{t("students.noClassroomOption")}</option>
        {classroomOptions.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        {keepsOldClassroom && <option value={student.classroomId ?? ""}>{student.classroomName ?? ""}</option>}
      </select>

      {!editing && (
        <>
          <label htmlFor="student-healthNote">{t("students.healthNote")}</label>
          <textarea id="student-healthNote" value={values.healthNote} onChange={(e) => setValues({ ...values, healthNote: e.target.value })}
            aria-invalid={errors.healthNote ? true : undefined} aria-describedby={described("healthNote")} />
          {error("healthNote")}
          <label htmlFor="student-allergy">{t("students.allergy")}</label>
          <textarea id="student-allergy" value={values.allergy} onChange={(e) => setValues({ ...values, allergy: e.target.value })}
            aria-invalid={errors.allergy ? true : undefined} aria-describedby={described("allergy")} />
          {error("allergy")}
        </>
      )}

      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("students.saving") : t("students.save")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("students.cancel")}</button>
      </div>
    </form>
  );
}
