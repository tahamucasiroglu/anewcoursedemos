// Öğrenci ve veli formlarının alan kuralları (spec 0009): sunucudaki kurallarla aynı; hatalı alan varsa istek gönderilmez.
// Sonuç metin değil anahtardır; ekran metni kendi tr.json'undan alır. Tarihler "yyyy-MM-dd" (tarih alanının değeri).
import { isValidPhone } from "../admin/adminForms";
import type { components } from "../schema";

export type StudentRow = components["schemas"]["StudentRow"];
export type StudentPage = components["schemas"]["StudentPage"];
export type StudentDetail = components["schemas"]["StudentDetail"];
export type StudentHealthView = components["schemas"]["StudentHealthView"];
export type StudentGuardianView = components["schemas"]["StudentGuardianView"];
export type SiblingRow = components["schemas"]["SiblingRow"];
export type PickupPersonView = components["schemas"]["PickupPersonView"];
export type ChildRow = components["schemas"]["ChildRow"];
export type ChildCard = components["schemas"]["ChildCard"];
export type LinkGuardianResult = components["schemas"]["LinkGuardianResult"];
export type StudentSaved = components["schemas"]["StudentSaved"];

export const STUDENT_NAME_MAX = 100;
export const SCHOOL_NUMBER_MAX = 20;
export const HEALTH_NOTE_MAX = 1000;
export const ALLERGY_MAX = 500;
export const GUARDIAN_MAX = 4;
export const PICKUP_MAX = 10;
export const PICKUP_RELATIONSHIP_MAX = 50;

export type GuardianRelationship = "Mother" | "Father" | "Other";
export const GUARDIAN_RELATIONSHIPS: readonly GuardianRelationship[] = ["Mother", "Father", "Other"];

export type StudentFieldError =
  | "fullNameRequired"
  | "fullNameTooLong"
  | "birthDateRequired"
  | "birthDateInFuture"
  | "birthDateTooOld"
  | "schoolNumberTooLong"
  | "schoolNumberInvalid"
  | "branchRequired"
  | "healthNoteTooLong"
  | "allergyTooLong"
  | "emailRequired"
  | "emailInvalid"
  | "phoneInvalid"
  | "phoneRequired"
  | "relationshipRequired"
  | "relationshipTooLong";

const DAY_PATTERN = /^\d{4}-\d{2}-\d{2}$/;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function nameError(value: string, max: number): StudentFieldError | undefined {
  const name = value.trim();
  if (name === "") {
    return "fullNameRequired";
  }

  return name.length > max ? "fullNameTooLong" : undefined;
}

/** "yyyy-MM-dd" günden yıl çıkarır (29 Şubat → 28 Şubat; sunucudaki DateOnly.AddYears ile aynı). */
function minusYears(day: string, years: number): string {
  const [y, m, d] = day.split("-").map(Number);
  const year = y - years;
  const leap = (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0;
  const dayOfMonth = m === 2 && d === 29 && !leap ? 28 : d;
  return `${String(year).padStart(4, "0")}-${String(m).padStart(2, "0")}-${String(dayOfMonth).padStart(2, "0")}`;
}

export interface StudentFormValues {
  fullName: string;
  birthDate: string;
  schoolNumber: string;
  branchId: string;
  classroomId: string;
  healthNote: string;
  allergy: string;
}

export type StudentFormErrors = Partial<Record<keyof StudentFormValues, StudentFieldError>>;

/** Öğrenci formu; <paramref name="today"/> Türkiye günü ("yyyy-MM-dd"). Sağlık alanları yalnızca eklemede gönderilir. */
export function validateStudentForm(values: StudentFormValues, today: string): StudentFormErrors {
  const errors: StudentFormErrors = {};
  const name = nameError(values.fullName, STUDENT_NAME_MAX);
  if (name) {
    errors.fullName = name;
  }

  const birth = values.birthDate.trim();
  if (!DAY_PATTERN.test(birth) || Number.isNaN(Date.parse(`${birth}T00:00:00Z`))) {
    errors.birthDate = "birthDateRequired";
  } else if (birth > today) {
    errors.birthDate = "birthDateInFuture";
  } else if (birth < minusYears(today, 100)) {
    errors.birthDate = "birthDateTooOld";
  }

  const number = values.schoolNumber.trim();
  if (number.length > SCHOOL_NUMBER_MAX) {
    errors.schoolNumber = "schoolNumberTooLong";
  } else if (number !== "" && !/^[A-Za-z0-9]+$/.test(number)) {
    errors.schoolNumber = "schoolNumberInvalid";
  }

  if (values.branchId === "") {
    errors.branchId = "branchRequired";
  }

  return { ...errors, ...validateHealthForm(values) };
}

export interface HealthFormValues {
  healthNote: string;
  allergy: string;
}

export function validateHealthForm(values: HealthFormValues): Partial<Record<keyof HealthFormValues, StudentFieldError>> {
  const errors: Partial<Record<keyof HealthFormValues, StudentFieldError>> = {};
  if (values.healthNote.trim().length > HEALTH_NOTE_MAX) {
    errors.healthNote = "healthNoteTooLong";
  }

  if (values.allergy.trim().length > ALLERGY_MAX) {
    errors.allergy = "allergyTooLong";
  }

  return errors;
}

export interface GuardianFormValues {
  fullName: string;
  email: string;
  phone: string;
  relationship: string;
}

export type GuardianFormErrors = Partial<Record<keyof GuardianFormValues, StudentFieldError>>;

export function validateGuardianForm(values: GuardianFormValues): GuardianFormErrors {
  const errors: GuardianFormErrors = {};
  const name = nameError(values.fullName, STUDENT_NAME_MAX);
  if (name) {
    errors.fullName = name;
  }

  const email = values.email.trim();
  if (email === "") {
    errors.email = "emailRequired";
  } else if (!EMAIL_PATTERN.test(email)) {
    errors.email = "emailInvalid";
  }

  if (!isValidPhone(values.phone)) {
    errors.phone = "phoneInvalid";
  }

  if (!(GUARDIAN_RELATIONSHIPS as readonly string[]).includes(values.relationship)) {
    errors.relationship = "relationshipRequired";
  }

  return errors;
}

export interface PickupPersonFormValues {
  fullName: string;
  relationship: string;
  phone: string;
}

export type PickupPersonFormErrors = Partial<Record<keyof PickupPersonFormValues, StudentFieldError>>;

export function validatePickupPersonForm(values: PickupPersonFormValues): PickupPersonFormErrors {
  const errors: PickupPersonFormErrors = {};
  const name = nameError(values.fullName, STUDENT_NAME_MAX);
  if (name) {
    errors.fullName = name;
  }

  const relationship = values.relationship.trim();
  if (relationship === "") {
    errors.relationship = "relationshipRequired";
  } else if (relationship.length > PICKUP_RELATIONSHIP_MAX) {
    errors.relationship = "relationshipTooLong";
  }

  if (values.phone.trim() === "") {
    errors.phone = "phoneRequired";
  } else if (!isValidPhone(values.phone)) {
    errors.phone = "phoneInvalid";
  }

  return errors;
}
