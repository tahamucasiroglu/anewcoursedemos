import { FlatList, Pressable, Text, View } from "react-native";
import { countMarks, fillText, saveButtonText, type AuthSession } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { AttendanceRow } from "./AttendanceRow";
import { useAttendanceSheet } from "./useAttendance";

interface Props {
  client: AuthSession["client"];
  classroomId: string;
  backLabel: string;
  onBack: () => void;
}

/**
 * Bir sınıfın bugünkü yoklaması (spec 0010, öğretmen): öğrenciler Türkçe ad sırasıyla, herkes "Var" (ya da kayıtlı
 * durumlar ve "Kaydedildi, saat SS:DD"); gelmeyenlere "Yok", geç kalana "Geç" ve tek "Kaydet (3 yok, 1 geç)" — onay
 * iletişimi yok. Sonuç ve hatalar duyurulur. Durumlar: yükleniyor, erişim yok, okul günü değil, öğrenci yok (kaydet kapalı),
 * kaydediliyor (düğme kilitli), kaydedildi, hata (işaretler korunur). Öğrenciler kayan listede (FlatList); sonuç, hata ve
 * Kaydet altta sabit (0010 review F-2). Liste bir sınıf kadardır; hepsi ilk çizimde hazır olur, ekran okuyucu ve dokunuş
 * sayısı (AC-5) kaydırmaya bağlı kalmaz.
 */
export function AttendanceScreen({ client, classroomId, backLabel, onBack }: Props) {
  const styles = useAuthStyles();
  const { state, retry, mark, save } = useAttendanceSheet(client, classroomId);
  const back = (
    <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
      <Text style={styles.secondaryText}>{backLabel}</Text>
    </Pressable>
  );

  if (state.kind === "loading") {
    return (
      <View style={styles.card} testID="attendance">
        <Text style={styles.notice}>{t("attendance.loading")}</Text>
      </View>
    );
  }

  if (state.kind === "error") {
    return (
      <View style={styles.card} testID="attendance">
        <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
        {!state.forbidden && (
          <Pressable style={styles.button} accessibilityRole="button" onPress={retry}>
            <Text style={styles.buttonText}>{t("attendance.retry")}</Text>
          </Pressable>
        )}
        {back}
      </View>
    );
  }

  const { sheet, marks, saving, result, saveError } = state;
  const counts = countMarks(marks);
  const empty = sheet.students.length === 0;
  const label = saving
    ? t("attendance.saving")
    : saveButtonText(counts, {
        plain: t("attendance.save"),
        withCounts: t("attendance.saveWith"),
        absent: t("attendance.saveAbsent"),
        late: t("attendance.saveLate"),
        separator: t("attendance.saveSeparator"),
      });
  return (
    <View style={[styles.card, styles.scrollCard]} testID="attendance">
      <Text style={styles.title} accessibilityRole="header">
        {sheet.classroomName}
      </Text>
      <Text style={styles.notice}>{sheet.branchName}</Text>
      {!sheet.isSchoolDay ? (
        <Text style={styles.text} accessibilityRole="alert">{t("attendance.notSchoolDay")}</Text>
      ) : (
        <>
          <Text style={styles.notice} testID="attendance-saved">
            {sheet.isSaved && sheet.savedTime ? fillText(t("attendance.savedAt"), { time: sheet.savedTime }) : t("attendance.notSaved")}
          </Text>
          {empty && <Text style={styles.text}>{t("attendance.noStudents")}</Text>}
          {!empty && (
            <FlatList
              testID="attendance-list"
              style={styles.scrollList}
              contentContainerStyle={styles.scrollContent}
              data={sheet.students}
              extraData={[marks, saving]}
              keyExtractor={(student) => student.studentId}
              initialNumToRender={sheet.students.length}
              renderItem={({ item: student }) => (
                <AttendanceRow
                  fullName={student.fullName}
                  status={marks[student.studentId] ?? "Present"}
                  disabled={saving}
                  onChange={(status) => mark(student.studentId, status)}
                />
              )}
            />
          )}
          {result && <Text style={styles.text} accessibilityRole="alert" accessibilityLiveRegion="polite">{result}</Text>}
          {saveError && <Text style={styles.error} accessibilityRole="alert">{saveError}</Text>}
          <Pressable
            style={[styles.button, (saving || empty) && styles.buttonDisabled]}
            accessibilityRole="button"
            accessibilityState={{ disabled: saving || empty, busy: saving }}
            disabled={saving || empty}
            onPress={() => void save()}
          >
            <Text style={styles.buttonText}>{label}</Text>
          </Pressable>
        </>
      )}
      {back}
    </View>
  );
}
