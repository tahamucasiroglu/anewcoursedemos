import { Pressable, ScrollView, Text, View } from "react-native";
import type { AuthSession } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useChildren } from "./useChildren";

interface Props {
  client: AuthSession["client"];
  onOpen: (id: string) => void;
  onBack: () => void;
}

/**
 * "Çocuklarım" (spec 0009 gereksinim 11, yalnızca veli, salt okunur): bağlı çocukların listesi — ad-soyad, şube ve sınıf
 * ("Sınıf atanmamış"); farklı şubelerdeki çocuklar tek listede. Durumlar: yükleniyor, boş ("Henüz bağlı öğrenci yok"),
 * dolu, hata + tekrar dene. Satıra dokununca salt okunur kart açılır; "Geri" Hoş geldin'e döner. Liste kayar (ScrollView),
 * "Geri" altta sabit (0010 review F-2).
 */
export function ChildrenScreen({ client, onOpen, onBack }: Props) {
  const styles = useAuthStyles();
  const { state, retry } = useChildren(client);
  return (
    <View style={[styles.card, styles.scrollCard]} testID="children">
      <Text style={styles.title} accessibilityRole="header">
        {t("children.title")}
      </Text>
      {state.kind === "loading" && <Text style={styles.notice}>{t("children.loading")}</Text>}
      {state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
          <Pressable style={styles.button} accessibilityRole="button" onPress={retry}>
            <Text style={styles.buttonText}>{t("children.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && state.children.length === 0 && <Text style={styles.text}>{t("children.empty")}</Text>}
      {state.kind === "ready" && state.children.length > 0 && (
        <ScrollView testID="children-list" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          {state.children.map((child) => {
            const classroom = child.classroomName ?? t("children.noClassroom");
            return (
              <Pressable
                key={child.id}
                style={styles.secondary}
                accessibilityRole="button"
                accessibilityLabel={`${child.fullName}, ${child.branchName}, ${classroom}, ${t("children.open")}`}
                testID="child-row"
                onPress={() => onOpen(child.id)}
              >
                <Text style={styles.label}>{child.fullName}</Text>
                <Text style={styles.notice}>{`${child.branchName} · ${classroom}`}</Text>
              </Pressable>
            );
          })}
        </ScrollView>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("children.back")}</Text>
      </Pressable>
    </View>
  );
}
