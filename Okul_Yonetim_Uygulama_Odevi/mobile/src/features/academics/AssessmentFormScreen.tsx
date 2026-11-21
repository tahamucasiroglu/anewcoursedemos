import { useState } from "react";
import { Pressable, ScrollView, Text, TextInput, View } from "react-native";
import {
  ASSESSMENT_KINDS,
  assessmentBody,
  callApi,
  toDayInput,
  turkeyToday,
  validateAssessmentForm,
  type AcademicsFieldError,
  type AssessmentFormErrors,
  type AssessmentFormValues,
  type AssessmentKind,
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

const ERROR_KEYS: Record<keyof AssessmentFormErrors, Partial<Record<AcademicsFieldError, TextKey>>> = {
  subject: { required: "academics.error.subject.required", tooLong: "academics.error.subject.tooLong" },
  title: { required: "academics.error.title.required", tooLong: "academics.error.assessmentTitle.tooLong" },
  date: { required: "academics.error.date.required", invalidDate: "academics.error.date.invalidDate", future: "academics.error.date.future" },
};

/**
 * "Yeni değerlendirme" formu (spec 0013 gereksinim 5): ders, tür (Yazılı / Sözlü — seçili olan ekran okuyucuya bildirilir),
 * başlık ve tarih ("GG.AA.YYYY", varsayılan bugün). Alan hataları istek gitmeden; dönem dışı tarih gibi sunucu hataları
 * duyurulur, girilenler kaybolmaz; kaydederken düğme kilitli; başarıda listeye döner. Form kayar (T16).
 */
export function AssessmentFormScreen({ client, classroomId, onDone, onCancel }: Props) {
  const styles = useAuthStyles();
  const today = turkeyToday();
  const [values, setValues] = useState<AssessmentFormValues>({ subject: "", kind: "Written", title: "", date: toDayInput(today) });
  const [errors, setErrors] = useState<AssessmentFormErrors>({});
  const [saving, setSaving] = useState(false);
  const [problem, setProblem] = useState<string | null>(null);

  const errorText = (field: keyof AssessmentFormErrors) => {
    const error = errors[field];
    const key = error ? ERROR_KEYS[field][error] : undefined;
    return key ? t(key) : undefined;
  };

  const submit = async () => {
    const found = validateAssessmentForm(values, today);
    setErrors(found);
    setProblem(null);
    if (Object.keys(found).length > 0 || saving) {
      return;
    }

    setSaving(true);
    const outcome = await callApi(client.POST("/api/academics/classrooms/{classroomId}/assessments", { params: { path: { classroomId } }, body: assessmentBody(values) }));
    setSaving(false);
    if (outcome.ok) {
      onDone();
    } else {
      setProblem(outcome.unreachable ? t("academics.unreachable") : outcome.message ?? t("academics.saveError"));
    }
  };

  const field = (name: "subject" | "title" | "date", label: string) => {
    const message = errorText(name);
    return (
      <View>
        <Text style={styles.label}>{label}</Text>
        <TextInput
          style={[styles.input, message !== undefined && styles.inputInvalid]}
          accessibilityLabel={label}
          accessibilityHint={message}
          value={values[name]}
          onChangeText={(text) => setValues((v) => ({ ...v, [name]: text }))}
          editable={!saving}
        />
        {message && (
          <Text style={styles.error} accessibilityRole="alert">{message}</Text>
        )}
      </View>
    );
  };

  const kindButton = (kind: AssessmentKind) => {
    const selected = values.kind === kind;
    const label = t(`academics.kind.${kind}` as TextKey);
    return (
      <Pressable
        key={kind}
        style={[styles.secondary, { alignSelf: "flex-start", minHeight: 44, minWidth: 88 }, selected && styles.button]}
        accessibilityRole="radio"
        accessibilityLabel={label}
        accessibilityState={{ checked: selected, disabled: saving }}
        disabled={saving}
        onPress={() => setValues((v) => ({ ...v, kind }))}
      >
        <Text style={selected ? styles.buttonText : styles.secondaryText}>{selected ? `✓ ${label}` : label}</Text>
      </Pressable>
    );
  };

  return (
    <View style={[styles.card, styles.scrollCard]} testID="assessment-form">
      <Text style={styles.title} accessibilityRole="header">
        {t("academics.newAssessment")}
      </Text>
      <ScrollView testID="assessment-form-scroll" style={styles.scrollList} contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled">
        {field("subject", t("academics.subject"))}
        <Text style={styles.label}>{t("academics.kindLabel")}</Text>
        <View style={styles.actions} accessibilityRole="radiogroup" accessibilityLabel={t("academics.kindLabel")}>
          {ASSESSMENT_KINDS.map(kindButton)}
        </View>
        {field("title", t("academics.assessmentTitle"))}
        {field("date", t("academics.date"))}
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
