import { Pressable, ScrollView, Text, View } from "react-native";
import { dayText, fillText, isInstallmentState, isPaymentMethod, moneyTl, toCents, type AuthSession } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useChildPayments } from "./usePayments";

interface Props {
  client: AuthSession["client"];
  studentId: string;
  onBack: () => void;
}

/**
 * Bir çocuğun plan ayrıntısı (spec 0015 gereksinim 10; salt okunur): her geçerli plan için net/ödenen/kalan/geciken, taksitler (vade,
 * tutar, durum — "Gecikti" metinle, renk tek başına anlam taşımaz) ve tahsilatlar (tarih, tutar, yöntem, makbuz no; iptal edilmişse
 * "İPTAL EDİLDİ"). Satır etiketleri ekran okuyucuya tek parça okunur. Durumlar: yükleniyor / hata + tekrar dene / "Bu öğrenci artık
 * görüntülenemiyor." (404) / plan yoksa "Henüz ödeme planı yok". İçerik kayar (T16), dönüş altta sabit.
 */
export function ChildPlanScreen({ client, studentId, onBack }: Props) {
  const styles = useAuthStyles();
  const { state, retry } = useChildPayments(client, studentId);

  return (
    <View style={[styles.card, styles.scrollCard]} testID="child-plan">
      {state.kind === "loading" && <Text style={styles.notice}>{t("payments.loading")}</Text>}
      {state.kind === "gone" && <Text style={styles.error} accessibilityRole="alert">{t("payments.gone")}</Text>}
      {state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
          <Pressable style={styles.button} accessibilityRole="button" onPress={retry}>
            <Text style={styles.buttonText}>{t("payments.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && (
        <ScrollView testID="child-plan-scroll" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          <Text style={styles.title} accessibilityRole="header">
            {state.child.fullName}
          </Text>
          {state.child.plans.length === 0 && <Text style={styles.text}>{t("payments.noPlan")}</Text>}
          {state.child.plans.map((plan) => (
            <View key={plan.id} style={styles.scrollContent} testID="child-plan-block">
              <Text style={styles.label}>{plan.feeName}</Text>
              <Text style={styles.text}>{fillText(t("payments.summary"), { net: moneyTl(plan.totals.net), paid: moneyTl(plan.totals.paid), remaining: moneyTl(plan.totals.remaining) })}</Text>
              {toCents(plan.totals.overdue) > 0 && <Text style={styles.error}>{fillText(t("payments.overdue"), { amount: moneyTl(plan.totals.overdue) })}</Text>}
              <Text style={styles.muted}>{t("payments.installments")}</Text>
              {plan.installments.map((i) => {
                const line = fillText(t("payments.installmentLine"), { seq: i.sequence, due: dayText(i.dueDate), amount: moneyTl(i.amount) });
                const stateLabel = isInstallmentState(i.state) ? t(`payments.state.${i.state}`) : i.state;
                return (
                  <View key={i.sequence} accessible accessibilityLabel={`${line}, ${stateLabel}`} testID="child-installment">
                    <Text style={styles.text}>{line}</Text>
                    <Text style={i.state === "Overdue" ? styles.error : styles.notice}>{stateLabel}</Text>
                  </View>
                );
              })}
              <Text style={styles.muted}>{t("payments.payments")}</Text>
              {plan.payments.length === 0 && <Text style={styles.notice}>{t("payments.noPayments")}</Text>}
              {plan.payments.map((p) => {
                const method = isPaymentMethod(p.method) ? t(`payments.method.${p.method}`) : p.method;
                const line = fillText(t("payments.paymentLine"), { date: dayText(p.date), amount: moneyTl(p.amount), method, no: p.receiptNo });
                return (
                  <View key={String(p.receiptNo)} accessible accessibilityLabel={p.cancelled ? `${line}, ${t("payments.cancelled")}` : line} testID="child-payment">
                    <Text style={styles.text}>{line}</Text>
                    {p.cancelled && <Text style={styles.error}>{t("payments.cancelled")}</Text>}
                  </View>
                );
              })}
            </View>
          ))}
        </ScrollView>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("payments.backToList")}</Text>
      </Pressable>
    </View>
  );
}
