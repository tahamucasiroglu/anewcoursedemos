import { Pressable, ScrollView, Text } from "react-native";
import { fillText, formatDay, type AuthSession } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useAssessments } from "./useAcademics";

interface Props {
  client: AuthSession["client"];
  classroomId: string;
  onNew: () => void;
  onOpen: (assessmentId: string) => void;
}

/**
 * Notlar sekmesi (spec 0013 gereksinim 6): sınıfın değerlendirmeleri tarihe göre yeniden eskiye — ders ve başlık, tür (Yazılı /
 * Sözlü), gün ve açan öğretmen, "x / y puan girildi". Satıra dokununca puan listesi açılır. Durumlar: yükleniyor, boş ("Henüz
 * değerlendirme yok"), dolu (kayan liste — T16), hata + tekrar dene.
 */
export function AssessmentsTab({ client, classroomId, onNew, onOpen }: Props) {
  const styles = useAuthStyles();
  const { state, retry } = useAssessments(client, classroomId);
  return (
    <>
      <Pressable style={styles.button} accessibilityRole="button" onPress={onNew}>
        <Text style={styles.buttonText}>{t("academics.newAssessment")}</Text>
      </Pressable>
      {state.kind === "loading" && <Text style={styles.notice}>{t("academics.loading")}</Text>}
      {state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
          <Pressable style={styles.secondary} accessibilityRole="button" onPress={retry}>
            <Text style={styles.secondaryText}>{t("academics.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && state.rows.length === 0 && <Text style={styles.text}>{t("academics.noAssessments")}</Text>}
      {state.kind === "ready" && state.rows.length > 0 && (
        <ScrollView testID="assessments-list" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          {state.rows.map((row) => {
            const heading = fillText(t("academics.assignmentRow"), { subject: row.subject, title: row.title });
            const meta = fillText(t("academics.assessmentLine"), { kind: t(`academics.kind.${row.kind}` as TextKey) ?? row.kind, date: formatDay(row.date), teacher: row.createdByName });
            const count = fillText(t("academics.gradedCount"), { x: Number(row.gradedCount), y: Number(row.studentCount) });
            return (
              <Pressable
                key={row.id}
                style={styles.secondary}
                accessibilityRole="button"
                accessibilityLabel={`${heading}, ${meta}, ${count}`}
                testID="assessment-row"
                onPress={() => onOpen(row.id)}
              >
                <Text style={styles.label}>{heading}</Text>
                <Text style={styles.notice}>{meta}</Text>
                <Text style={styles.notice}>{count}</Text>
              </Pressable>
            );
          })}
        </ScrollView>
      )}
    </>
  );
}
