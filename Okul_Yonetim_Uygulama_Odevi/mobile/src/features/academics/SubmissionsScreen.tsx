import { useMemo } from "react";
import { FlatList, Pressable, StyleSheet, Text, View } from "react-native";
import { fillText, formatDay, SUBMISSION_STATUSES, submissionsButtonText, type AuthSession, type SubmissionStatus } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useTheme } from "../../theme/useTheme";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useSubmissions } from "./useAcademics";

interface Props {
  client: AuthSession["client"];
  assignmentId: string;
  onBack: () => void;
}

/**
 * Teslim listesi (spec 0013 gereksinim 4): ödevin öğrencileri Türkçe ad sırasıyla, her biri için Teslim etti / Teslim etmedi /
 * İşaretlenmedi (tek dokunuş; etiket öğrenci adını içerir — "Ayşe Yılmaz: Teslim etmedi" —, seçili olan bildirilir ve ✓ ile
 * yazılır; "Teslim etmedi" yalnızca renkle değil yazıyla) ve tek "Kaydet (3 etmedi)" — onay iletişimi yok. Başka öğretmenin
 * ödevi salt okunur ("Yalnızca {öğretmen} değiştirebilir", düğme yok). Durumlar: yükleniyor, erişim yok, öğrenci yok,
 * kaydediliyor (düğme kilitli), kaydedildi, hata (işaretler korunur). Öğrenciler kayan listede (T16), Kaydet altta sabit.
 */
export function SubmissionsScreen({ client, assignmentId, onBack }: Props) {
  const styles = useAuthStyles();
  const { colors } = useTheme();
  const rowStyles = useMemo(
    () =>
      StyleSheet.create({
        row: { gap: 6, paddingVertical: 6, borderTopWidth: 1, borderColor: colors.border },
        name: { fontSize: 16, color: colors.text },
        choices: { flexDirection: "row", flexWrap: "wrap", gap: 8 },
        choice: { minWidth: 44, minHeight: 44, justifyContent: "center", alignItems: "center", paddingHorizontal: 10, borderRadius: 6, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface },
        chosen: { backgroundColor: colors.primary, borderColor: colors.primary },
        choiceText: { fontSize: 15, color: colors.text },
        chosenText: { fontSize: 15, fontWeight: "600", color: colors.onPrimary },
      }),
    [colors],
  );
  const { state, retry, mark, save, counts } = useSubmissions(client, assignmentId);
  const back = (
    <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
      <Text style={styles.secondaryText}>{t("academics.backToList")}</Text>
    </Pressable>
  );

  if (state.kind === "loading") {
    return (
      <View style={styles.card} testID="submissions">
        <Text style={styles.notice}>{t("academics.loading")}</Text>
      </View>
    );
  }

  if (state.kind === "error") {
    return (
      <View style={styles.card} testID="submissions">
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

  const { view, marks, saving, result, saveError } = state;
  const empty = view.students.length === 0;
  const label = saving
    ? t("academics.saving")
    : submissionsButtonText(counts!, { plain: t("academics.save"), withNotSubmitted: t("academics.saveWithNotSubmitted") });
  return (
    <View style={[styles.card, styles.scrollCard]} testID="submissions">
      <Text style={styles.title} accessibilityRole="header">
        {fillText(t("academics.assignmentRow"), { subject: view.subject, title: view.title })}
      </Text>
      <Text style={styles.notice}>{fillText(t("academics.dueLine"), { due: formatDay(view.dueDate), teacher: view.createdByName })}</Text>
      {!view.canEdit && <Text style={styles.text} testID="read-only">{fillText(t("academics.readOnly"), { teacher: view.createdByName })}</Text>}
      {empty && <Text style={styles.text}>{t("academics.noStudents")}</Text>}
      {!empty && (
        <FlatList
          testID="submissions-list"
          style={styles.scrollList}
          contentContainerStyle={styles.scrollContent}
          data={view.students}
          extraData={[marks, saving]}
          keyExtractor={(student) => student.studentId}
          initialNumToRender={view.students.length}
          renderItem={({ item: student }) => {
            const current: SubmissionStatus = marks[student.studentId] ?? "Unmarked";
            const disabled = saving || !view.canEdit;
            return (
              <View style={rowStyles.row} testID="submission-row" accessibilityRole="radiogroup" accessibilityLabel={student.fullName}>
                <Text style={rowStyles.name}>{student.fullName}</Text>
                <View style={rowStyles.choices}>
                  {SUBMISSION_STATUSES.map((option) => {
                    const text = t(`academics.status.${option}` as TextKey);
                    const selected = option === current;
                    return (
                      <Pressable
                        key={option}
                        style={[rowStyles.choice, selected && rowStyles.chosen]}
                        accessibilityRole="radio"
                        accessibilityLabel={`${student.fullName}: ${text}`}
                        accessibilityState={{ checked: selected, disabled }}
                        disabled={disabled}
                        onPress={() => mark(student.studentId, option)}
                      >
                        <Text style={selected ? rowStyles.chosenText : rowStyles.choiceText}>{selected ? `✓ ${text}` : text}</Text>
                      </Pressable>
                    );
                  })}
                </View>
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
          <Text style={styles.buttonText}>{label}</Text>
        </Pressable>
      )}
      {back}
    </View>
  );
}
