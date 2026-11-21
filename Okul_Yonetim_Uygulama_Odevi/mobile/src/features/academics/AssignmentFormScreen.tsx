import { useState } from "react";
import { Pressable, ScrollView, Text, TextInput, View } from "react-native";
import {
  addDays,
  assignmentBody,
  callApi,
  DESCRIPTION_MAX,
  toDayInput,
  turkeyToday,
  validateAssignmentForm,
  type AcademicsFieldError,
  type AssignmentFormErrors,
  type AssignmentFormValues,
  type AuthSession,
} from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";

interface Props {
  client: AuthSession["client"];
  classroomId: string;
  onDone: () => void;
  onCancel: () => void;
}

const ERROR_KEYS: Record<keyof AssignmentFormErrors, Partial<Record<AcademicsFieldError, TextKey>>> = {
  subject: { required: "academics.error.subject.required", tooLong: "academics.error.subject.tooLong" },
  title: { required: "academics.error.title.required", tooLong: "academics.error.assignmentTitle.tooLong" },
  description: { tooLong: "academics.error.description.tooLong" },
  dueDate: { required: "academics.error.date.required", invalidDate: "academics.error.date.invalidDate", dueOutOfRange: "academics.error.date.dueOutOfRange" },
};

/**
 * "Yeni ödev" formu (spec 0013 gereksinim 1): ders, başlık, isteğe bağlı açıklama ve teslim tarihi ("GG.AA.YYYY", varsayılan
 * yarın; "Bugün" / "Yarın" kısayolları — tarih seçici paketi yok, plan Risks 7). Alan hataları istek gitmeden gösterilir;
 * kaydederken düğme kilitli; sunucu hatası duyurulur ve girilenler kaybolmaz; başarıda listeye döner. Form kayar (T16).
 */
export function AssignmentFormScreen({ client, classroomId, onDone, onCancel }: Props) {
  const styles = useAuthStyles();
  const today = turkeyToday();
  const [values, setValues] = useState<AssignmentFormValues>({ subject: "", title: "", description: "", dueDate: toDayInput(addDays(today, 1)) });
  const [errors, setErrors] = useState<AssignmentFormErrors>({});
  const [saving, setSaving] = useState(false);
  const [problem, setProblem] = useState<string | null>(null);

  const set = (field: keyof AssignmentFormValues) => (text: string) => setValues((v) => ({ ...v, [field]: text }));
  const errorText = (field: keyof AssignmentFormErrors) => {
    const error = errors[field];
    const key = error ? ERROR_KEYS[field][error] : undefined;
    return key ? t(key) : undefined;
  };

  const submit = async () => {
    const found = validateAssignmentForm(values, today);
    setErrors(found);
    setProblem(null);
    if (Object.keys(found).length > 0 || saving) {
      return;
    }

    setSaving(true);
    const outcome = await callApi(client.POST("/api/academics/classrooms/{classroomId}/assignments", { params: { path: { classroomId } }, body: assignmentBody(values) }));
    setSaving(false);
    if (outcome.ok) {
      onDone();
    } else {
      setProblem(outcome.unreachable ? t("academics.unreachable") : outcome.message ?? t("academics.saveError"));
    }
  };

  const field = (name: keyof AssignmentFormValues, label: string, options: { multiline?: boolean; maxLength?: number } = {}) => {
    const message = errorText(name);
    return (
      <View>
        <Text style={styles.label}>{label}</Text>
        <TextInput
          style={[styles.input, message !== undefined && styles.inputInvalid]}
          accessibilityLabel={label}
          accessibilityHint={message}
          value={values[name]}
          onChangeText={set(name)}
          editable={!saving}
          multiline={options.multiline}
          maxLength={options.maxLength}
        />
        {message && (
          <Text style={styles.error} accessibilityRole="alert">{message}</Text>
        )}
      </View>
    );
  };

  return (
    <View style={[styles.card, styles.scrollCard]} testID="assignment-form">
      <Text style={styles.title} accessibilityRole="header">
        {t("academics.newAssignment")}
      </Text>
      <ScrollView testID="assignment-form-scroll" style={styles.scrollList} contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled">
        {field("subject", t("academics.subject"))}
        {field("title", t("academics.assignmentTitle"))}
        {field("description", t("academics.description"), { multiline: true, maxLength: DESCRIPTION_MAX + 100 })}
        {field("dueDate", t("academics.dueDate"))}
        <View style={styles.actions}>
          <Pressable style={styles.secondary} accessibilityRole="button" disabled={saving} onPress={() => set("dueDate")(toDayInput(today))}>
            <Text style={styles.secondaryText}>{t("academics.today")}</Text>
          </Pressable>
          <Pressable style={styles.secondary} accessibilityRole="button" disabled={saving} onPress={() => set("dueDate")(toDayInput(addDays(today, 1)))}>
            <Text style={styles.secondaryText}>{t("academics.tomorrow")}</Text>
          </Pressable>
        </View>
      </ScrollView>
      {problem && <Text style={styles.error} accessibilityRole="alert">{problem}</Text>}
      <View style={styles.actions}>
        <Pressable
          style={[styles.button, saving && styles.buttonDisabled]}
          accessibilityRole="button"
          accessibilityState={{ disabled: saving, busy: saving }}
          disabled={saving}
          onPress={() => void submit()}
        >
          <Text style={styles.buttonText}>{saving ? t("academics.saving") : t("academics.save")}</Text>
        </Pressable>
        <Pressable style={styles.secondary} accessibilityRole="button" disabled={saving} onPress={onCancel}>
          <Text style={styles.secondaryText}>{t("academics.cancel")}</Text>
        </Pressable>
      </View>
    </View>
  );
}
