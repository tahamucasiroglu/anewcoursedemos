import { useEffect, useState } from "react";
import { Pressable, Text, View } from "react-native";
import { calendarKindKey, callApi, dateRangeText, type AuthSession, type SchoolCalendarView } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; calendar: SchoolCalendarView };

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
}

/**
 * Okul takvimi (spec 0008, mobil, her rol, salt okunur): güncel dönemin adı ve tarihleri (yaklaşan dönemse "Yaklaşan
 * dönem"), kayıtlar (tür metni, başlık, tek gün ya da tarih aralığı) tarih sırasıyla. Dönem yoksa "Henüz dönem
 * tanımlanmadı.", kayıt yoksa "Bu dönemde takvim kaydı yok."; yükleniyor ve hata + tekrar dene; "Geri" Hoş geldin'e döner.
 */
export function CalendarScreen({ client, onBack }: Props) {
  const styles = useAuthStyles();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/calendar")).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setState({ kind: "ready", calendar: outcome.data });
      } else {
        setState({ kind: "error", message: outcome.unreachable ? t("auth.unreachable") : outcome.message ?? t("calendar.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  const term = state.kind === "ready" ? state.calendar.term : null;
  return (
    <View style={styles.card} testID="calendar">
      <Text style={styles.title} accessibilityRole="header">
        {t("calendar.title")}
      </Text>
      {state.kind === "loading" && <Text style={styles.notice}>{t("calendar.loading")}</Text>}
      {state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
          <Pressable
            style={styles.button}
            accessibilityRole="button"
            onPress={() => {
              setState({ kind: "loading" });
              setReload((n) => n + 1);
            }}
          >
            <Text style={styles.buttonText}>{t("calendar.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && !term && <Text style={styles.text}>{t("calendar.noTerm")}</Text>}
      {state.kind === "ready" && term && (
        <>
          <Text style={styles.label} accessibilityRole="header">{term.name}</Text>
          <Text style={styles.notice}>
            {dateRangeText(term.startDate, term.endDate)}
            {term.isUpcoming ? ` · ${t("calendar.upcoming")}` : ""}
          </Text>
          {state.calendar.entries.length === 0 ? (
            <Text style={styles.text}>{t("calendar.empty")}</Text>
          ) : (
            state.calendar.entries.map((entry) => {
              const kind = t(`calendar.kind.${calendarKindKey(entry.kind)}` as TextKey);
              const when = dateRangeText(entry.startDate, entry.endDate);
              return (
                <View key={entry.id} accessible accessibilityLabel={`${kind}, ${entry.title}, ${when}`} testID="calendar-entry">
                  <Text style={styles.muted}>{kind}</Text>
                  <Text style={styles.text}>{entry.title}</Text>
                  <Text style={styles.notice}>{when}</Text>
                </View>
              );
            })
          )}
        </>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("calendar.back")}</Text>
      </Pressable>
    </View>
  );
}
