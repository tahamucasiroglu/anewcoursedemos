import { Pressable, ScrollView, Text } from "react-native";
import { fillText, formatDay, type AuthSession } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useAssignments } from "./useAcademics";

interface Props {
  client: AuthSession["client"];
  classroomId: string;
  onNew: () => void;
  onOpen: (assignmentId: string) => void;
}

/**
 * Ödevler sekmesi (spec 0013 gereksinim 3): sınıfın ödevleri teslim tarihine göre yeniden eskiye — ders ve başlık, teslim günü ve
 * veren öğretmen, "x / y teslim etti". Satıra dokununca teslim listesi açılır. Durumlar: yükleniyor, boş ("Henüz ödev yok"),
 * dolu (kayan liste — T16), hata + tekrar dene. "Yeni ödev" listenin dışında, üstte sabit.
 */
export function AssignmentsTab({ client, classroomId, onNew, onOpen }: Props) {
  const styles = useAuthStyles();
  const { state, retry } = useAssignments(client, classroomId);
  return (
    <>
      <Pressable style={styles.button} accessibilityRole="button" onPress={onNew}>
        <Text style={styles.buttonText}>{t("academics.newAssignment")}</Text>
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
      {state.kind === "ready" && state.rows.length === 0 && <Text style={styles.text}>{t("academics.noAssignments")}</Text>}
      {state.kind === "ready" && state.rows.length > 0 && (
        <ScrollView testID="assignments-list" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          {state.rows.map((row) => {
            const heading = fillText(t("academics.assignmentRow"), { subject: row.subject, title: row.title });
            const due = fillText(t("academics.dueLine"), { due: formatDay(row.dueDate), teacher: row.createdByName });
            const count = fillText(t("academics.submittedCount"), { x: Number(row.submittedCount), y: Number(row.studentCount) });
            return (
              <Pressable
                key={row.id}
                style={styles.secondary}
                accessibilityRole="button"
                accessibilityLabel={`${heading}, ${due}, ${count}`}
                testID="assignment-row"
                onPress={() => onOpen(row.id)}
              >
                <Text style={styles.label}>{heading}</Text>
                <Text style={styles.notice}>{due}</Text>
                <Text style={styles.notice}>{count}</Text>
              </Pressable>
            );
          })}
        </ScrollView>
      )}
    </>
  );
}
