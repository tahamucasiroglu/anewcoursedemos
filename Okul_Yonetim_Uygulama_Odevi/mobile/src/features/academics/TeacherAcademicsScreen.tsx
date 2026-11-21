import { useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import type { AuthSession } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useMyClassrooms } from "../attendance/useAttendance";
import { useAuthStyles } from "../auth/useAuthStyles";
import { AssessmentFormScreen } from "./AssessmentFormScreen";
import { AssessmentsTab } from "./AssessmentsTab";
import { AssignmentFormScreen } from "./AssignmentFormScreen";
import { AssignmentsTab } from "./AssignmentsTab";
import { GradesScreen } from "./GradesScreen";
import { SubmissionsScreen } from "./SubmissionsScreen";

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
}

type Tab = "assignments" | "assessments";
type Sub = { kind: "assignmentForm" } | { kind: "submissions"; id: string } | { kind: "assessmentForm" } | { kind: "grades"; id: string } | null;

/**
 * "Ödev ve not" (spec 0013, yalnızca öğretmen): atanmış sınıflar (0010'un Sınıflarım listesi; tek sınıfta doğrudan açılır),
 * sonra iki sekme — Ödevler | Notlar. Ödevler: liste → "Yeni ödev" formu ya da ödeve dokununca teslim listesi; Notlar: liste →
 * "Yeni değerlendirme" formu ya da puan listesi. Alt ekrandan dönünce liste yeniden yüklenir (sekme yeniden çizilir). Sınıf
 * seçiminin durumları: yükleniyor, boş ("Size atanmış sınıf yok"), hata + tekrar dene. Yönlendirme kütüphanesi yok: ekran
 * durumla seçilir (plan Risks 7).
 */
export function TeacherAcademicsScreen({ client, onBack }: Props) {
  const styles = useAuthStyles();
  const { state, retry } = useMyClassrooms(client);
  const [selected, setSelected] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>("assignments");
  const [sub, setSub] = useState<Sub>(null);
  const rows = state.kind === "ready" ? state.rows : [];
  const only = rows.length === 1 ? rows[0] : null;
  const open = rows.find((r) => r.id === selected) ?? only;

  if (open) {
    const close = () => setSub(null);
    switch (sub?.kind) {
      case "assignmentForm":
        return <AssignmentFormScreen key="new-assignment" client={client} classroomId={open.id} onDone={close} onCancel={close} />;
      case "submissions":
        return <SubmissionsScreen key={sub.id} client={client} assignmentId={sub.id} onBack={close} />;
      case "assessmentForm":
        return <AssessmentFormScreen key="new-assessment" client={client} classroomId={open.id} onDone={close} onCancel={close} />;
      case "grades":
        return <GradesScreen key={sub.id} client={client} assessmentId={sub.id} onBack={close} />;
      default:
        break;
    }

    const back = only
      ? onBack
      : () => {
          setSelected(null);
          setTab("assignments");
          retry();
        };
    const tabButton = (value: Tab, label: string) => (
      <Pressable
        style={[styles.secondary, { alignSelf: "flex-start", minHeight: 44 }, tab === value && styles.button]}
        accessibilityRole="tab"
        accessibilityState={{ selected: tab === value }}
        onPress={() => setTab(value)}
      >
        <Text style={tab === value ? styles.buttonText : styles.secondaryText}>{label}</Text>
      </Pressable>
    );
    return (
      <View style={[styles.card, styles.scrollCard]} testID="teacher-academics">
        <Text style={styles.title} accessibilityRole="header">
          {open.name}
        </Text>
        <Text style={styles.notice}>{open.branchName}</Text>
        <View style={styles.actions} accessibilityRole="tablist">
          {tabButton("assignments", t("academics.tabAssignments"))}
          {tabButton("assessments", t("academics.tabAssessments"))}
        </View>
        {tab === "assignments" ? (
          <AssignmentsTab key={`a-${open.id}`} client={client} classroomId={open.id} onNew={() => setSub({ kind: "assignmentForm" })} onOpen={(id) => setSub({ kind: "submissions", id })} />
        ) : (
          <AssessmentsTab key={`n-${open.id}`} client={client} classroomId={open.id} onNew={() => setSub({ kind: "assessmentForm" })} onOpen={(id) => setSub({ kind: "grades", id })} />
        )}
        <Pressable style={styles.secondary} onPress={back} accessibilityRole="button">
          <Text style={styles.secondaryText}>{only ? t("academics.back") : t("academics.backToClassrooms")}</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <View style={[styles.card, styles.scrollCard]} testID="academics-classrooms">
      <Text style={styles.title} accessibilityRole="header">
        {t("academics.title")}
      </Text>
      {state.kind === "loading" && <Text style={styles.notice}>{t("academics.loading")}</Text>}
      {state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
          <Pressable style={styles.button} accessibilityRole="button" onPress={retry}>
            <Text style={styles.buttonText}>{t("academics.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && rows.length === 0 && <Text style={styles.text}>{t("academics.noClassrooms")}</Text>}
      {rows.length > 1 && (
        <ScrollView testID="academics-classrooms-list" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          {rows.map((row) => (
            <Pressable
              key={row.id}
              style={styles.secondary}
              accessibilityRole="button"
              accessibilityLabel={`${row.name}, ${row.branchName}, ${t("academics.open")}`}
              onPress={() => setSelected(row.id)}
            >
              <Text style={styles.label}>{row.name}</Text>
              <Text style={styles.notice}>{row.branchName}</Text>
            </Pressable>
          ))}
        </ScrollView>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("academics.back")}</Text>
      </Pressable>
    </View>
  );
}
