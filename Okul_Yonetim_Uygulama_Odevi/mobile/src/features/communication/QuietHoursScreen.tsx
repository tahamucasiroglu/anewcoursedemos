import { useEffect, useState } from "react";
import { Pressable, ScrollView, Text, TextInput, View } from "react-native";
import {
  callApi,
  quietHoursBody,
  toggleDay,
  validateQuietHours,
  WEEK_DAYS,
  type AuthSession,
  type CommunicationFieldError,
  type QuietHoursFormErrors,
  type WeekDay,
} from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useCommunicationStyles } from "./useCommunicationStyles";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; enabled: boolean };
const DEFAULT_DAYS: WeekDay[] = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
}

/**
 * "Mesai saatlerim" (spec 0011 gereksinim 6; AC-9, AC-13 — öğretmen): başlangıç ve bitiş "SS:DD" (Türkiye saati, 15 dk
 * adım), gün düğmeleri (Pzt…Paz; erişilebilirlik etiketi tam gün adı ve seçili durumu). Alan hatası varsa istek gitmez;
 * kaydedince "Kaydedildi"; "Sessiz modu kapat" ile kapatılır; yükleme hatasında "Tekrar dene". Saat seçici paketi yok (plan Risks 7). İçerik kayar, "Geri"
 * altta sabit.
 */
export function QuietHoursScreen({ client, onBack }: Props) {
  const styles = useAuthStyles();
  const own = useCommunicationStyles();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [start, setStart] = useState("08:00");
  const [end, setEnd] = useState("17:00");
  const [days, setDays] = useState<WeekDay[]>(DEFAULT_DAYS);
  const [errors, setErrors] = useState<QuietHoursFormErrors>({});
  const [saving, setSaving] = useState(false);
  const [result, setResult] = useState<{ error: boolean; text: string } | null>(null);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/me/quiet-hours")).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        if (outcome.data.enabled) {
          setStart(outcome.data.start ?? "");
          setEnd(outcome.data.end ?? "");
          setDays(WEEK_DAYS.filter((d) => outcome.data.days.includes(d)));
        }

        setState({ kind: "ready", enabled: outcome.data.enabled });
      } else {
        setState({ kind: "error", message: outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("quiet.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  const failure = (outcome: { unreachable: boolean; message?: string | null }) =>
    setResult({ error: true, text: outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("quiet.saveError") });

  const save = async () => {
    const found = validateQuietHours({ start, end, days });
    setErrors(found);
    setResult(null);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    const outcome = await callApi(client.PUT("/api/me/quiet-hours", { body: quietHoursBody({ start, end, days }) }));
    setSaving(false);
    if (outcome.ok) {
      setState({ kind: "ready", enabled: true });
      setResult({ error: false, text: t("quiet.saved") });
    } else {
      failure(outcome);
    }
  };

  const disable = async () => {
    setSaving(true);
    setResult(null);
    const outcome = await callApi(client.PUT("/api/me/quiet-hours", { body: { enabled: false, start: null, end: null, days: null } }));
    setSaving(false);
    if (outcome.ok) {
      setState({ kind: "ready", enabled: false });
      setResult({ error: false, text: t("quiet.disabled") });
    } else {
      failure(outcome);
    }
  };

  const fieldText = (error: CommunicationFieldError) => t(`quiet.field.${error}` as TextKey);
  const timeField = (label: string, value: string, onChange: (v: string) => void, error: CommunicationFieldError | undefined) => (
    <>
      <Text style={styles.label}>{label}</Text>
      <TextInput
        style={[styles.input, error && styles.inputInvalid]}
        accessibilityLabel={label}
        accessibilityHint={error ? fieldText(error) : undefined}
        value={value}
        onChangeText={onChange}
        keyboardType="numbers-and-punctuation"
        maxLength={5}
      />
      {error && (
        <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
          {fieldText(error)}
        </Text>
      )}
    </>
  );

  return (
    <View style={[styles.card, styles.scrollCard]} testID="quiet-hours">
      <Text style={styles.title} accessibilityRole="header">
        {t("quiet.title")}
      </Text>
      {state.kind === "loading" && <Text style={styles.notice}>{t("quiet.loading")}</Text>}
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
            <Text style={styles.buttonText}>{t("communication.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && (
        <ScrollView style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          <Text style={styles.notice}>{state.enabled ? t("quiet.on") : t("quiet.off")}</Text>
          {timeField(t("quiet.start"), start, setStart, errors.start)}
          {timeField(t("quiet.end"), end, setEnd, errors.end)}
          <Text style={styles.label}>{t("quiet.days")}</Text>
          <View style={own.choices}>
            {WEEK_DAYS.map((day) => {
              const chosen = days.includes(day);
              return (
                <Pressable
                  key={day}
                  style={[own.choice, chosen && own.chosen]}
                  accessibilityRole="button"
                  accessibilityLabel={t(`day.${day}` as TextKey)}
                  accessibilityState={{ selected: chosen }}
                  onPress={() => setDays((current) => toggleDay(current, day))}
                >
                  <Text style={chosen ? own.chosenText : own.choiceText}>{t(`day.short.${day}` as TextKey)}</Text>
                </Pressable>
              );
            })}
          </View>
          {errors.days && (
            <Text style={styles.error} accessibilityRole="alert">
              {fieldText(errors.days)}
            </Text>
          )}
          <View style={styles.actions}>
            <Pressable
              style={[styles.button, saving && styles.buttonDisabled]}
              accessibilityRole="button"
              accessibilityState={{ disabled: saving, busy: saving }}
              disabled={saving}
              onPress={() => void save()}
            >
              <Text style={styles.buttonText}>{saving ? t("quiet.saving") : t("quiet.save")}</Text>
            </Pressable>
            {state.enabled && (
              <Pressable style={styles.secondary} accessibilityRole="button" disabled={saving} accessibilityState={{ disabled: saving }} onPress={() => void disable()}>
                <Text style={styles.secondaryText}>{t("quiet.disable")}</Text>
              </Pressable>
            )}
          </View>
        </ScrollView>
      )}
      {result && (
        <Text style={result.error ? styles.error : styles.notice} accessibilityRole={result.error ? "alert" : "text"} accessibilityLiveRegion="polite">
          {result.text}
        </Text>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("quiet.back")}</Text>
      </Pressable>
    </View>
  );
}
