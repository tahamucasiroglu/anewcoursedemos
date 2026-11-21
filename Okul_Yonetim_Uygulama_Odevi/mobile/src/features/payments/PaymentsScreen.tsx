import { useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import { fillText, moneyTl, toCents, type AuthSession } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { ChildPlanScreen } from "./ChildPlanScreen";
import { useMyPayments } from "./usePayments";

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
}

/**
 * Velinin "Ödemeler" ekranı (spec 0015 gereksinim 10, UX; salt okunur — ödeme düğmesi yok): en üstte çocukların toplam kalanı, sonra
 * çocuk kartları (ad, şube, kalan, geciken — etiket ekran okuyucuya tek parça) → çocuğun plan ayrıntısı. Hiçbir çocuğun planı yoksa
 * "Henüz ödeme planı yok". Durumlar: yükleniyor / hata + "Tekrar dene". Liste kayar (T16), dönüş altta sabit.
 */
export function PaymentsScreen({ client, onBack }: Props) {
  const styles = useAuthStyles();
  const { state, retry } = useMyPayments(client);
  const [open, setOpen] = useState<string | null>(null);

  if (open) {
    return <ChildPlanScreen key={open} client={client} studentId={open} onBack={() => setOpen(null)} />;
  }

  const children = state.kind === "ready" ? state.view.children : [];
  const anyPlan = children.some((c) => c.plans.length > 0);
  return (
    <View style={[styles.card, styles.scrollCard]} testID="payments">
      <Text style={styles.title} accessibilityRole="header">
        {t("payments.title")}
      </Text>
      {state.kind === "loading" && <Text style={styles.notice}>{t("payments.loading")}</Text>}
      {state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
          <Pressable style={styles.button} accessibilityRole="button" onPress={retry}>
            <Text style={styles.buttonText}>{t("payments.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && !anyPlan && <Text style={styles.text}>{t("payments.noPlan")}</Text>}
      {state.kind === "ready" && anyPlan && (
        <>
          <Text style={styles.label} testID="payments-total">{fillText(t("payments.totalRemaining"), { amount: moneyTl(state.view.totalRemaining) })}</Text>
          <ScrollView testID="payments-list" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
            {children.map((child) => (
              <Pressable
                key={child.studentId}
                style={styles.secondary}
                accessibilityRole="button"
                accessibilityLabel={fillText(t("payments.childLabel"), { name: child.fullName, branch: child.branchName, remaining: moneyTl(child.totals.remaining) })}
                onPress={() => setOpen(child.studentId)}
                testID="payments-child"
              >
                <Text style={styles.label}>{child.fullName}</Text>
                <Text style={styles.notice}>{child.classroomName ? `${child.branchName} / ${child.classroomName}` : child.branchName}</Text>
                {child.plans.length === 0 ? (
                  <Text style={styles.notice}>{t("payments.noPlan")}</Text>
                ) : (
                  <Text style={styles.text}>{fillText(t("payments.summary"), { net: moneyTl(child.totals.net), paid: moneyTl(child.totals.paid), remaining: moneyTl(child.totals.remaining) })}</Text>
                )}
                {toCents(child.totals.overdue) > 0 && <Text style={styles.error}>{fillText(t("payments.childOverdue"), { amount: moneyTl(child.totals.overdue) })}</Text>}
              </Pressable>
            ))}
          </ScrollView>
        </>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("payments.back")}</Text>
      </Pressable>
    </View>
  );
}
