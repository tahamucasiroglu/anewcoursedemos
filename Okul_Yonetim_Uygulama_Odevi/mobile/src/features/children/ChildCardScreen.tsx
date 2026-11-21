import { useEffect, useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import { callApi, formatDay, type AuthSession, type ChildCard } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";

type State = { kind: "loading" } | { kind: "gone" } | { kind: "error"; message: string } | { kind: "ready"; child: ChildCard };

interface Props {
  client: AuthSession["client"];
  childId: string;
  onBack: () => void;
  onAcademics?: () => void;
}

/**
 * Velinin gördüğü salt okunur öğrenci kartı (spec 0009 C-7): ad-soyad, doğum tarihi, okul numarası, şube, sınıf ("Sınıf
 * atanmamış"), Sağlık bölümü (sağlık notu ve alerji ayrı başlıklarla; anahtar yoksa "Sağlık bilgisi şu anda
 * gösterilemiyor.") ve teslim alabilecek kişiler ("Henüz kimse eklenmedi"). Bağ kaldırıldıysa ya da öğrenci ayrıldıysa
 * (404) "Bu öğrenci artık görüntülenemiyor." ve listeye dönüş. Diğer veliler ve kardeş kartları burada yoktur. Kart içeriği
 * kayar (ScrollView), listeye dönüş altta sabit (0010 review F-2). Spec 0013: kart açıkken "Ödev ve notlar" o çocuğun özetini açar.
 */
export function ChildCardScreen({ client, childId, onBack, onAcademics }: Props) {
  const styles = useAuthStyles();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/my/children/{id}", { params: { path: { id: childId } } })).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setState({ kind: "ready", child: outcome.data });
      } else if (!outcome.unreachable && outcome.status === 404) {
        setState({ kind: "gone" });
      } else {
        setState({ kind: "error", message: outcome.unreachable ? t("auth.unreachable") : outcome.message ?? t("children.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, childId, reload]);

  const fact = (label: string, value: string) => (
    <View accessible accessibilityLabel={`${label}: ${value}`}>
      <Text style={styles.muted}>{label}</Text>
      <Text style={styles.text}>{value}</Text>
    </View>
  );

  return (
    <View style={[styles.card, styles.scrollCard]} testID="child-card">
      {state.kind === "loading" && <Text style={styles.notice}>{t("children.loading")}</Text>}
      {state.kind === "gone" && <Text style={styles.error} accessibilityRole="alert">{t("children.gone")}</Text>}
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
            <Text style={styles.buttonText}>{t("children.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && (
        <ScrollView testID="child-card-scroll" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          <Text style={styles.title} accessibilityRole="header">
            {state.child.fullName}
          </Text>
          {fact(t("children.birthDate"), formatDay(state.child.birthDate))}
          {state.child.schoolNumber && fact(t("children.schoolNumber"), state.child.schoolNumber)}
          {fact(t("children.branch"), state.child.branchName)}
          {fact(t("children.classroom"), state.child.classroomName ?? t("children.noClassroom"))}

          <Text style={styles.label} accessibilityRole="header">{t("children.health")}</Text>
          {state.child.health.available ? (
            <>
              <Text style={styles.muted} accessibilityRole="header">{t("children.healthNote")}</Text>
              <Text style={styles.text}>{state.child.health.healthNote ?? t("children.healthEmpty")}</Text>
              <Text style={styles.muted} accessibilityRole="header">{t("children.allergy")}</Text>
              <Text style={styles.text}>{state.child.health.allergy ?? t("children.healthEmpty")}</Text>
            </>
          ) : (
            <Text style={styles.notice}>{t("children.healthUnavailable")}</Text>
          )}

          <Text style={styles.label} accessibilityRole="header">{t("children.pickupPersons")}</Text>
          {state.child.pickupPersons.length === 0 ? (
            <Text style={styles.text}>{t("children.noPickupPersons")}</Text>
          ) : (
            state.child.pickupPersons.map((person, index) => (
              <View key={`${person.phone}-${index}`} accessible accessibilityLabel={`${person.fullName}, ${person.relationship}, ${person.phone}`} testID="pickup-person">
                <Text style={styles.text}>{`${person.fullName} (${person.relationship})`}</Text>
                <Text style={styles.notice}>{person.phone}</Text>
              </View>
            ))
          )}
        </ScrollView>
      )}
      {state.kind === "ready" && onAcademics && (
        <Pressable style={styles.secondary} onPress={onAcademics} accessibilityRole="button">
          <Text style={styles.secondaryText}>{t("childAcademics.link")}</Text>
        </Pressable>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("children.backToList")}</Text>
      </Pressable>
    </View>
  );
}
