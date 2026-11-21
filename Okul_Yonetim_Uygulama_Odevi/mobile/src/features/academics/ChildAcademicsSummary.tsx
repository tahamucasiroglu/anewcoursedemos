import { Pressable, ScrollView, Text, View } from "react-native";
import { fillText, formatDay, type AuthSession, type StudentAssignmentRecord, type StudentGradeRecord } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useChildAcademics } from "./useAcademics";

interface Props {
  client: AuthSession["client"];
  studentId: string;
  backLabel: string;
  onBack: () => void;
}

/**
 * Velinin bir çocuk için ödev ve not özeti (spec 0013 C-7): üç bölüm, bölüm bazında boş yazısı, yükleniyor / hata + tekrar dene /
 * "Bu öğrenci artık görüntülenemiyor." (404). Satır etiketleri ekran okuyucuya tek parça okunur.
 */
export function ChildAcademicsSummary({ client, studentId, backLabel, onBack }: Props) {
  const styles = useAuthStyles();
  const { state, retry } = useChildAcademics(client, studentId);

  const assignment = (row: StudentAssignmentRecord, withStatus: boolean) => {
    const line = fillText(t("childAcademics.assignmentLine"), { subject: row.subject, title: row.title, due: formatDay(row.dueDate) });
    const status = t(`academics.status.${row.status}` as TextKey) ?? row.status;
    return (
      <View key={row.assignmentId} accessible accessibilityLabel={withStatus ? `${line}, ${status}` : line} testID="summary-assignment">
        <Text style={styles.text}>{line}</Text>
        {withStatus && <Text style={row.status === "NotSubmitted" ? styles.error : styles.notice}>{status}</Text>}
      </View>
    );
  };

  const grade = (row: StudentGradeRecord) => {
    const score = row.score === null || row.score === undefined ? t("childAcademics.notEntered") : String(row.score);
    const line = fillText(t("childAcademics.gradeLine"), { subject: row.subject, title: row.title, score });
    const meta = fillText(t("childAcademics.gradeMeta"), { kind: t(`academics.kind.${row.kind}` as TextKey) ?? row.kind, date: formatDay(row.date) });
    return (
      <View key={row.assessmentId} accessible accessibilityLabel={`${line}, ${meta}`} testID="summary-grade">
        <Text style={styles.text}>{line}</Text>
        <Text style={styles.notice}>{meta}</Text>
      </View>
    );
  };

  return (
    <View style={[styles.card, styles.scrollCard]} testID="child-academics">
      {state.kind === "loading" && <Text style={styles.notice}>{t("academics.loading")}</Text>}
      {state.kind === "gone" && <Text style={styles.error} accessibilityRole="alert">{t("childAcademics.gone")}</Text>}
      {state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
          <Pressable style={styles.button} accessibilityRole="button" onPress={retry}>
            <Text style={styles.buttonText}>{t("academics.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && (
        <ScrollView testID="child-academics-scroll" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          <Text style={styles.title} accessibilityRole="header">
            {state.view.fullName}
          </Text>
          <Text style={styles.label} accessibilityRole="header">{t("childAcademics.pending")}</Text>
          {state.view.pending.length === 0 ? <Text style={styles.notice}>{t("childAcademics.noPending")}</Text> : state.view.pending.map((row) => assignment(row, false))}
          <Text style={styles.label} accessibilityRole="header">{t("childAcademics.recent")}</Text>
          {state.view.recent.length === 0 ? <Text style={styles.notice}>{t("childAcademics.noRecent")}</Text> : state.view.recent.map((row) => assignment(row, true))}
          <Text style={styles.label} accessibilityRole="header">{t("childAcademics.grades")}</Text>
          {state.view.grades.length === 0 ? <Text style={styles.notice}>{t("childAcademics.noGrades")}</Text> : state.view.grades.map(grade)}
        </ScrollView>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{backLabel}</Text>
      </Pressable>
    </View>
  );
}
