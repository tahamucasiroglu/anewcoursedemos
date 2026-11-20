import { useEffect, useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import {
  callApi,
  type AuthSession,
  type ChildTeachers,
  type ClassroomContacts,
  type ContactPerson,
  type ConversationRow,
  type MessageContacts,
  type StudentContacts,
} from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; contacts: MessageContacts };
type Step =
  | { kind: "root" }
  | { kind: "child"; child: ChildTeachers }
  | { kind: "classroom"; classroom: ClassroomContacts }
  | { kind: "student"; classroom: ClassroomContacts; student: StudentContacts };

interface Props {
  client: AuthSession["client"];
  onStarted: (conversation: ConversationRow) => void;
  onBack: () => void;
}

/**
 * "Yeni mesaj" (spec 0011 UX): veli çocuk → öğretmen, öğretmen sınıf → öğrenci → veli seçer (iki rolü olan ikisini de görür);
 * seçilen kişiyle o öğrenci için konuşma açılır (varsa var olan döner). Seçenekler kayar (ScrollView), "Geri" altta sabit.
 */
export function NewConversationScreen({ client, onStarted, onBack }: Props) {
  const styles = useAuthStyles();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [step, setStep] = useState<Step>({ kind: "root" });
  const [starting, setStarting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/conversations/contacts")).then((outcome) => {
      if (active) {
        setState(
          outcome.ok
            ? { kind: "ready", contacts: outcome.data }
            : { kind: "error", message: outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("contacts.loadError") },
        );
      }
    });
    return () => {
      active = false;
    };
  }, [client]);

  const start = async (studentId: string, person: ContactPerson) => {
    setStarting(true);
    setError(null);
    const outcome = await callApi(client.POST("/api/conversations", { body: { studentId, counterpartUserId: person.userId } }));
    setStarting(false);
    if (outcome.ok) {
      onStarted(outcome.data);
    } else {
      setError(outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("contacts.startError"));
    }
  };

  const option = (key: string, label: string, onPress: () => void) => (
    <Pressable key={key} style={styles.secondary} accessibilityRole="button" disabled={starting} accessibilityState={{ disabled: starting }} onPress={onPress}>
      <Text style={styles.secondaryText}>{label}</Text>
    </Pressable>
  );

  const body = () => {
    if (state.kind !== "ready") {
      return null;
    }

    const { asGuardian, asTeacher } = state.contacts;
    switch (step.kind) {
      case "child":
        return (
          <>
            <Text style={styles.label}>{t("contacts.pickTeacher")}</Text>
            {step.child.teachers.length === 0 && <Text style={styles.text}>{t("contacts.noTeacher")}</Text>}
            {step.child.teachers.map((teacher) => option(teacher.userId, teacher.fullName, () => void start(step.child.studentId, teacher)))}
          </>
        );
      case "classroom":
        return (
          <>
            <Text style={styles.label}>{t("contacts.pickStudent")}</Text>
            {step.classroom.students.map((student) =>
              option(student.studentId, student.studentName, () => setStep({ kind: "student", classroom: step.classroom, student })),
            )}
          </>
        );
      case "student":
        return (
          <>
            <Text style={styles.label}>{t("contacts.pickGuardian")}</Text>
            {step.student.guardians.length === 0 && <Text style={styles.text}>{t("contacts.noGuardian")}</Text>}
            {step.student.guardians.map((guardian) => option(guardian.userId, guardian.fullName, () => void start(step.student.studentId, guardian)))}
          </>
        );
      default:
        if (asGuardian.length === 0 && asTeacher.length === 0) {
          return <Text style={styles.text}>{t("contacts.empty")}</Text>;
        }

        return (
          <>
            {asGuardian.length > 0 && <Text style={styles.label}>{t("contacts.pickChild")}</Text>}
            {asGuardian.map((child) => option(child.studentId, child.studentName, () => setStep({ kind: "child", child })))}
            {asTeacher.length > 0 && <Text style={styles.label}>{t("contacts.pickClassroom")}</Text>}
            {asTeacher.map((classroom) => option(classroom.classroomId, classroom.classroomName, () => setStep({ kind: "classroom", classroom })))}
          </>
        );
    }
  };

  const back = () => {
    if (step.kind === "student") {
      setStep({ kind: "classroom", classroom: step.classroom });
    } else if (step.kind === "root") {
      onBack();
    } else {
      setStep({ kind: "root" });
    }
  };

  return (
    <View style={[styles.card, styles.scrollCard]} testID="new-conversation">
      <Text style={styles.title} accessibilityRole="header">
        {t("contacts.title")}
      </Text>
      {state.kind === "loading" && <Text style={styles.notice}>{t("contacts.loading")}</Text>}
      {state.kind === "error" && <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>}
      {state.kind === "ready" && (
        <ScrollView testID="contacts-list" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          {body()}
        </ScrollView>
      )}
      {starting && <Text style={styles.notice}>{t("contacts.starting")}</Text>}
      {error && <Text style={styles.error} accessibilityRole="alert">{error}</Text>}
      <Pressable style={styles.secondary} onPress={back} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("contacts.back")}</Text>
      </Pressable>
    </View>
  );
}
