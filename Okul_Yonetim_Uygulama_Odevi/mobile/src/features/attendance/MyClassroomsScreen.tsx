import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import type { AuthSession } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { AttendanceScreen } from "./AttendanceScreen";
import { useMyClassrooms } from "./useAttendance";

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
}

/**
 * "Yoklama al" (spec 0010, yalnızca öğretmen): Sınıflarım — atanmış sınıflar şube ve bugünkü durumuyla ("Bugün alındı" /
 * "Bugün alınmadı"). Tek sınıf varsa doğrudan o sınıfın yoklaması açılır (geri Hoş geldin'e döner); birden çoksa satıra
 * dokununca açılır, geri listeye döner ve liste yenilenir. Durumlar: yükleniyor, boş ("Size atanmış sınıf yok"), dolu,
 * hata + tekrar dene.
 */
export function MyClassroomsScreen({ client, onBack }: Props) {
  const styles = useAuthStyles();
  const { state, retry } = useMyClassrooms(client);
  const [selected, setSelected] = useState<string | null>(null);
  const only = state.kind === "ready" && state.rows.length === 1 ? state.rows[0].id : null;
  const open = selected ?? only;

  if (open) {
    return only ? (
      <AttendanceScreen key={open} client={client} classroomId={open} backLabel={t("attendance.back")} onBack={onBack} />
    ) : (
      <AttendanceScreen
        key={open}
        client={client}
        classroomId={open}
        backLabel={t("attendance.backToList")}
        onBack={() => {
          setSelected(null);
          retry();
        }}
      />
    );
  }

  return (
    <View style={styles.card} testID="my-classrooms">
      <Text style={styles.title} accessibilityRole="header">
        {t("attendance.title")}
      </Text>
      {state.kind === "loading" && <Text style={styles.notice}>{t("attendance.loading")}</Text>}
      {state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
          <Pressable style={styles.button} accessibilityRole="button" onPress={retry}>
            <Text style={styles.buttonText}>{t("attendance.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && state.rows.length === 0 && <Text style={styles.text}>{t("attendance.noClassrooms")}</Text>}
      {state.kind === "ready" &&
        state.rows.map((row) => {
          const status = row.takenToday ? t("attendance.taken") : t("attendance.notTaken");
          return (
            <Pressable
              key={row.id}
              style={styles.secondary}
              accessibilityRole="button"
              accessibilityLabel={`${row.name}, ${row.branchName}, ${status}, ${t("attendance.open")}`}
              testID="classroom-row"
              onPress={() => setSelected(row.id)}
            >
              <Text style={styles.label}>{row.name}</Text>
              <Text style={styles.notice}>{`${row.branchName} · ${status}`}</Text>
            </Pressable>
          );
        })}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("attendance.back")}</Text>
      </Pressable>
    </View>
  );
}
