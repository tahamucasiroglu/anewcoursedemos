import { useRef } from "react";
import { FlatList, Pressable, Text, TextInput, View } from "react-native";
import { fillText, formatDay, type AuthSession } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useGrades } from "./useAcademics";

interface Props {
  client: AuthSession["client"];
  assessmentId: string;
  onBack: () => void;
}

/**
 * Puan listesi (spec 0013 gereksinim 6): değerlendirmenin öğrencileri Türkçe ad sırasıyla, her birine sayı klavyesiyle 0–100
 * arası puan ya da boş (girilmedi); "sonraki" tuşu bir sonraki öğrenciye geçer. Alan etiketi öğrenci adını içerir ("Ayşe Yılmaz
 * puanı"). Geçersiz puan istek gitmeden o öğrencinin alanında gösterilir; tek "Kaydet". Başka öğretmenin değerlendirmesi salt
 * okunur. Durumlar: yükleniyor, erişim yok, öğrenci yok, kaydediliyor (düğme kilitli), kaydedildi, hata (puanlar korunur).
 * Öğrenciler kayan listede (T16), Kaydet altta sabit.
 */
export function GradesScreen({ client, assessmentId, onBack }: Props) {
  const styles = useAuthStyles();
  const { state, retry, setScore, save } = useGrades(client, assessmentId);
  const inputs = useRef<Record<string, { focus: () => void } | null>>({});
  const back = (
    <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
      <Text style={styles.secondaryText}>{t("academics.backToList")}</Text>
    </Pressable>
  );

  if (state.kind === "loading") {
    return (
      <View style={styles.card} testID="grades">
        <Text style={styles.notice}>{t("academics.loading")}</Text>
      </View>
    );
  }

  if (state.kind === "error") {
    return (
      <View style={styles.card} testID="grades">
        <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
        {!state.forbidden && (
          <Pressable style={styles.button} accessibilityRole="button" onPress={retry}>
            <Text style={styles.buttonText}>{t("academics.retry")}</Text>
          </Pressable>
        )}
        {back}
      </View>
    );
  }

  const { view, scores, invalid, saving, result, saveError } = state;
  const empty = view.students.length === 0;
  const kind = t(`academics.kind.${view.kind}` as TextKey) ?? view.kind;
  return (
    <View style={[styles.card, styles.scrollCard]} testID="grades">
      <Text style={styles.title} accessibilityRole="header">
        {fillText(t("academics.assignmentRow"), { subject: view.subject, title: view.title })}
      </Text>
      <Text style={styles.notice}>{fillText(t("academics.assessmentLine"), { kind, date: formatDay(view.date), teacher: view.createdByName })}</Text>
      {!view.canEdit && <Text style={styles.text} testID="read-only">{fillText(t("academics.readOnly"), { teacher: view.createdByName })}</Text>}
      {empty && <Text style={styles.text}>{t("academics.noGradeStudents")}</Text>}
      {!empty && (
        <FlatList
          testID="grades-list"
          style={styles.scrollList}
          contentContainerStyle={styles.scrollContent}
          data={view.students}
          extraData={[scores, invalid, saving]}
          keyExtractor={(student) => student.studentId}
          initialNumToRender={view.students.length}
          keyboardShouldPersistTaps="handled"
          renderItem={({ item: student, index }) => {
            const label = fillText(t("academics.scoreLabel"), { name: student.fullName });
            const wrong = invalid.includes(student.studentId);
            const next = view.students[index + 1];
            return (
              <View testID="grade-row">
                <Text style={styles.label}>{student.fullName}</Text>
                <TextInput
                  ref={(input) => {
                    inputs.current[student.studentId] = input;
                  }}
                  style={[styles.input, { minHeight: 44 }, wrong && styles.inputInvalid]}
                  accessibilityLabel={label}
                  accessibilityHint={wrong ? t("academics.scoreInvalid") : undefined}
                  value={scores[student.studentId] ?? ""}
                  onChangeText={(text) => setScore(student.studentId, text)}
                  editable={view.canEdit && !saving}
                  keyboardType="number-pad"
                  maxLength={3}
                  returnKeyType={next ? "next" : "done"}
                  submitBehavior={next ? "submit" : "blurAndSubmit"}
                  onSubmitEditing={() => (next ? inputs.current[next.studentId]?.focus() : undefined)}
                />
                {wrong && (
                  <Text style={styles.error} accessibilityRole="alert">{t("academics.scoreInvalid")}</Text>
                )}
              </View>
            );
          }}
        />
      )}
      {result && <Text style={styles.text} accessibilityRole="alert" accessibilityLiveRegion="polite">{result}</Text>}
      {saveError && <Text style={styles.error} accessibilityRole="alert">{saveError}</Text>}
      {view.canEdit && !empty && (
        <Pressable
          style={[styles.button, saving && styles.buttonDisabled]}
          accessibilityRole="button"
          accessibilityState={{ disabled: saving, busy: saving }}
          disabled={saving}
          onPress={() => void save()}
        >
          <Text style={styles.buttonText}>{saving ? t("academics.saving") : t("academics.save")}</Text>
        </Pressable>
      )}
      {back}
    </View>
  );
}
