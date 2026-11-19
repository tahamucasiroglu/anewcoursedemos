import { Pressable, Text, View } from "react-native";
import type { TenantSummary } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "./useAuthStyles";

interface Props {
  tenants: TenantSummary[];
  selecting: boolean;
  failed: boolean;
  onSelect: (tenantId: string) => void;
}

/** Mobil okul seçimi (spec 0004): okul düğmeleri; seçilirken kilitli; hata duyurulur. */
export function SchoolSelectScreen({ tenants, selecting, failed, onSelect }: Props) {
  const styles = useAuthStyles();
  return (
    <View style={styles.card} testID="school-select" accessibilityState={{ busy: selecting }}>
      <Text style={styles.title} accessibilityRole="header">
        {t("school.title")}
      </Text>
      {tenants.length === 0 && <Text style={styles.text}>{t("school.none")}</Text>}
      {tenants.map((tenant) => (
        <Pressable
          key={tenant.id}
          style={styles.secondary}
          onPress={() => onSelect(tenant.id)}
          disabled={selecting}
          accessibilityRole="button"
          accessibilityState={{ disabled: selecting }}
        >
          <Text style={styles.secondaryText}>{tenant.name}</Text>
        </Pressable>
      ))}
      {selecting && <Text style={styles.notice}>{t("school.loading")}</Text>}
      {failed && (
        <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
          {t("school.error")}
        </Text>
      )}
    </View>
  );
}
