import { useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import type { AuthSession } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useChildren } from "../children/useChildren";
import { ChildAcademicsSummary } from "./ChildAcademicsSummary";

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
  /** Çocuk kartından gelindiyse o çocuk (seçim atlanır). */
  childId?: string;
}

/**
 * "Ödev ve notlar" (spec 0013 gereksinim 7, yalnızca veli): [birden çok çocukta] çocuk seçimi ("Çocuklarım" listesi; tek çocukta
 * ya da çocuk kartından gelince doğrudan), sonra çocuğun özeti — Bekleyen ödevler (en yakın üstte), Son ödevler (son 30 gün,
 * durumuyla; "Teslim etmedi" yazıyla) ve Notlar (yeniden eskiye; puan ya da "girilmedi"). Durumlar: yükleniyor, bölüm bazında boş
 * ("Bekleyen ödev yok", "Son 30 günde ödev yok", "Henüz not yok"), hata + tekrar dene, "Bu öğrenci artık görüntülenemiyor.";
 * çocuk seçiminde boş ("Kayıtlı çocuğunuz yok"). Özet kayar (T16), dönüş altta sabit.
 */
export function ChildAcademicsScreen({ client, onBack, childId }: Props) {
  const styles = useAuthStyles();
  const children = useChildren(client);
  const [selected, setSelected] = useState<string | null>(childId ?? null);
  const list = children.state.kind === "ready" ? children.state.children : [];
  const only = !childId && list.length === 1 ? list[0].id : null;
  const open = selected ?? only;

  if (open) {
    const back = childId || only ? onBack : () => setSelected(null);
    return <ChildAcademicsSummary key={open} client={client} studentId={open} backLabel={childId || only ? t("childAcademics.back") : t("childAcademics.backToChildren")} onBack={back} />;
  }

  return (
    <View style={[styles.card, styles.scrollCard]} testID="child-academics-children">
      <Text style={styles.title} accessibilityRole="header">
        {t("childAcademics.title")}
      </Text>
      {children.state.kind === "loading" && <Text style={styles.notice}>{t("academics.loading")}</Text>}
      {children.state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{children.state.message}</Text>
          <Pressable style={styles.button} accessibilityRole="button" onPress={children.retry}>
            <Text style={styles.buttonText}>{t("academics.retry")}</Text>
          </Pressable>
        </>
      )}
      {children.state.kind === "ready" && list.length === 0 && <Text style={styles.text}>{t("childAcademics.noChildren")}</Text>}
      {list.length > 1 && (
        <ScrollView testID="child-academics-list" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          {list.map((child) => (
            <Pressable
              key={child.id}
              style={styles.secondary}
              accessibilityRole="button"
              accessibilityLabel={`${child.fullName}, ${child.branchName}, ${t("academics.open")}`}
              onPress={() => setSelected(child.id)}
            >
              <Text style={styles.label}>{child.fullName}</Text>
              <Text style={styles.notice}>{child.branchName}</Text>
            </Pressable>
          ))}
        </ScrollView>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("childAcademics.back")}</Text>
      </Pressable>
    </View>
  );
}
