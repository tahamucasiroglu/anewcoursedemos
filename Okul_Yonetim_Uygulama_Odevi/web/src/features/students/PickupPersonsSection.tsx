import { useState, type FormEvent } from "react";
import {
  PICKUP_MAX,
  validatePickupPersonForm,
  type AuthSession,
  type PickupPersonFormErrors,
  type PickupPersonFormValues,
  type PickupPersonView,
} from "@okul/api-client";
import { t } from "../../i18n/t";
import { failureText } from "../admin/adminText";
import { call } from "./studentsApi";
import { studentFieldText } from "./studentsText";

const EMPTY: PickupPersonFormValues = { fullName: "", relationship: "", phone: "" };

interface Props {
  client: AuthSession["client"];
  studentId: string;
  persons: PickupPersonView[];
  onChanged: () => void;
}

/**
 * Kartın "Teslim alabilecek kişiler" bölümü (spec 0009 gereksinim 10): liste (ad, yakınlık, telefon), "Kişi ekle",
 * satırda "Düzenle" ve "Kaldır"; her değişiklik bütün listeyi kaydeder. Ad, yakınlık ve geçerli telefon zorunlu (alan
 * hataları istek göndermeden); 10 kişide "Kişi ekle" pasif ve nedeni yazılı. Form düzenlenen satıra bağlı anahtarla
 * kurulur (T15): başka satırda "Düzenle"ye basınca o satırın değerleri gelir.
 */
export function PickupPersonsSection({ client, studentId, persons, onChanged }: Props) {
  const [editing, setEditing] = useState<{ index: number | null } | null>(null);
  const [values, setValues] = useState(EMPTY);
  const [errors, setErrors] = useState<PickupPersonFormErrors>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const save = async (next: PickupPersonView[]) => {
    setSaving(true);
    setServerError(null);
    const outcome = await call(client.PUT("/api/admin/students/{id}/pickup-persons", {
      params: { path: { id: studentId } },
      body: { persons: next.map((p) => ({ fullName: p.fullName, relationship: p.relationship, phone: p.phone })) },
    }));
    setSaving(false);
    if (outcome.ok) {
      onChanged();
    } else {
      setServerError(failureText(outcome));
    }
  };

  const open = (index: number | null) => {
    setEditing({ index });
    setValues(index === null ? EMPTY : { fullName: persons[index].fullName, relationship: persons[index].relationship, phone: persons[index].phone });
    setErrors({});
    setServerError(null);
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validatePickupPersonForm(values);
    setErrors(found);
    if (Object.keys(found).length > 0 || !editing) {
      return;
    }

    const person = { fullName: values.fullName.trim(), relationship: values.relationship.trim(), phone: values.phone.trim() };
    await save(editing.index === null ? [...persons, person] : persons.map((p, i) => (i === editing.index ? person : p)));
  };

  const full = persons.length >= PICKUP_MAX;
  const field = (name: keyof PickupPersonFormValues, label: string, type = "text") => {
    const inputId = `pickup-${name}`;
    const error = errors[name];
    return (
      <>
        <label htmlFor={inputId}>{label}</label>
        <input id={inputId} type={type} value={values[name]} onChange={(e) => setValues({ ...values, [name]: e.target.value })}
          aria-invalid={error ? true : undefined} aria-describedby={error ? `${inputId}-error` : undefined} />
        {error && <p id={`${inputId}-error`} className="field-error" role="alert">{studentFieldText(error)}</p>}
      </>
    );
  };

  return (
    <div>
      <h4>{t("students.pickupPersons")}</h4>
      {persons.length === 0 ? (
        <p className="notice">{t("students.noPickupPersons")}</p>
      ) : (
        <ul className="admin-list">
          {persons.map((p, index) => (
            <li key={`${p.fullName}-${p.phone}-${index}`} className="admin-row">
              <span>
                <strong>{p.fullName}</strong> <span className="notice">({p.relationship})</span>
                <br />
                <span className="notice">{p.phone}</span>
              </span>
              <span className="actions">
                <button type="button" className="secondary" disabled={saving} onClick={() => open(index)} aria-label={`${p.fullName} — ${t("students.edit")}`}>{t("students.edit")}</button>
                <button type="button" className="secondary" disabled={saving} onClick={() => void save(persons.filter((_, i) => i !== index))} aria-label={`${p.fullName} — ${t("students.removePickupPerson")}`}>
                  {t("students.removePickupPerson")}
                </button>
              </span>
            </li>
          ))}
        </ul>
      )}
      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      {editing ? (
        <form key={editing.index ?? "new"} className="auth-form" onSubmit={submit} noValidate aria-label={editing.index === null ? t("students.addPickupPerson") : t("students.edit")}>
          {field("fullName", t("students.fullName"))}
          {field("relationship", t("students.pickupRelationship"))}
          {field("phone", t("students.phone"), "tel")}
          <div className="actions">
            <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("students.saving") : t("students.save")}</button>
            <button type="button" className="secondary" onClick={() => setEditing(null)}>{t("students.cancel")}</button>
          </div>
        </form>
      ) : (
        <div className="actions">
          <button type="button" disabled={full} aria-describedby={full ? "pickup-limit" : undefined} onClick={() => open(null)}>{t("students.addPickupPerson")}</button>
          {full && <span id="pickup-limit" className="notice">{t("students.pickupLimit")}</span>}
        </div>
      )}
    </div>
  );
}
