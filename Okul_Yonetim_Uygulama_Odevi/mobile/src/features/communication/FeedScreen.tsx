import { useCallback, useEffect, useState } from "react";
import { FlatList, Pressable, RefreshControl, Text, View } from "react-native";
import { callApi, formatDay, turkeyToday, type AuthSession, type FeedItem } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { AnnouncementDetailScreen } from "./AnnouncementDetailScreen";
import { useCommunicationStyles } from "./useCommunicationStyles";
import { WeeklySummaryNoticeScreen } from "./WeeklySummaryNoticeScreen";

type State =
  | { kind: "loading" }
  | { kind: "error"; message: string }
  | { kind: "ready"; items: FeedItem[]; page: number; hasMore: boolean };

const feedKey = (item: FeedItem) => `${item.kind}:${item.id}`;

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
}

/**
 * Velinin duyuru akışı (spec 0011 gereksinim 2, 7; AC-13): alıcısı olduğu duyurular ve çocuklarının okul bildirimleri,
 * yeniden eskiye, 20'lik sayfalar ("Daha fazla"). Okunmamış satırda "Yeni" işareti (erişilebilirlik etiketi "okunmadı");
 * okul bildiriminin yayınlayanı "Okul bildirimi". Duyuruya ve haftalık gelişim özetine (spec 0014, tür `WeeklySummary` — başlık
 * "Haftalık gelişim özeti — 16–22 Kasım", çocuk adıyla) dokununca ayrıntı açılır (sunucu okundu yazar); dönünce akış
 * yenilenir. Açılınca ve aşağı çekince yenilenir; "Daha fazla" satırları `tür:kimlik` ile tekrarsız ekler, hata verirse not
 * gösterir. Durumlar: yükleniyor, boş ("Henüz duyuru yok"), dolu, hata + tekrar dene.
 * Liste kayar (FlatList), "Geri" altta sabit (conventions T16).
 */
export function FeedScreen({ client, onBack }: Props) {
  const styles = useAuthStyles();
  const own = useCommunicationStyles();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [refreshing, setRefreshing] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [moreError, setMoreError] = useState<string | null>(null);
  const [openId, setOpenId] = useState<string | null>(null);
  const [openSummaryId, setOpenSummaryId] = useState<string | null>(null);

  const failure = useCallback(
    (outcome: { unreachable: boolean; message?: string | null }) =>
      outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("feed.loadError"),
    [],
  );

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/announcements/feed", { params: { query: { page: 1 } } })).then((outcome) => {
      if (!active) {
        return;
      }

      setRefreshing(false);
      setMoreError(null);
      if (outcome.ok) {
        setState({ kind: "ready", items: outcome.data.items, page: 1, hasMore: outcome.data.hasMore });
      } else {
        setState({ kind: "error", message: failure(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload, failure]);

  if (openSummaryId) {
    return (
      <WeeklySummaryNoticeScreen
        key={openSummaryId}
        client={client}
        noticeId={openSummaryId}
        onBack={() => {
          setOpenSummaryId(null);
          setReload((n) => n + 1);
        }}
      />
    );
  }

  if (openId) {
    return (
      <AnnouncementDetailScreen
        key={openId}
        client={client}
        announcementId={openId}
        onBack={() => {
          setOpenId(null);
          setReload((n) => n + 1);
        }}
      />
    );
  }

  const loadMore = async () => {
    if (state.kind !== "ready") {
      return;
    }

    const from = state.page;
    setLoadingMore(true);
    setMoreError(null);
    const outcome = await callApi(client.GET("/api/announcements/feed", { params: { query: { page: from + 1 } } }));
    setLoadingMore(false);
    if (!outcome.ok) {
      setMoreError(failure(outcome));
      return;
    }

    // Arada yenilenmiş akışın üzerine eski sayfa yazılmaz; sayfalar arasında kayan satır iki kez görünmez (review L-6).
    setState((current) => {
      if (current.kind !== "ready" || current.page !== from) {
        return current;
      }

      const seen = new Set(current.items.map(feedKey));
      return { kind: "ready", items: [...current.items, ...outcome.data.items.filter((item) => !seen.has(feedKey(item)))], page: from + 1, hasMore: outcome.data.hasMore };
    });
  };

  const renderItem = ({ item }: { item: FeedItem }) => {
    const author = item.authorName ?? t("feed.school");
    const day = formatDay(turkeyToday(new Date(item.at)));
    const meta = [author, item.childNames.join(", "), day].filter((part) => part !== "").join(" · ");
    const label = [item.isRead ? null : t("feed.unreadLabel"), item.requiresConsent ? t("feed.consent") : null, item.title, meta].filter(Boolean).join(", ");
    const content = (
      <>
        {!item.isRead && (
          <Text style={own.badge} accessibilityLabel={t("feed.unreadLabel")}>
            {t("feed.new")}
          </Text>
        )}
        <Text style={item.isRead ? styles.text : styles.label}>{item.title}</Text>
        <Text style={styles.notice}>{item.requiresConsent ? `${t("feed.consent")} · ${meta}` : meta}</Text>
      </>
    );
    return item.kind === "Announcement" || item.kind === "WeeklySummary" ? (
      <Pressable
        style={own.row}
        testID="feed-row"
        accessibilityRole="button"
        accessibilityLabel={label}
        onPress={() => (item.kind === "WeeklySummary" ? setOpenSummaryId(item.id) : setOpenId(item.id))}
      >
        {content}
      </Pressable>
    ) : (
      <View style={own.row} testID="feed-row" accessible accessibilityLabel={label}>
        {content}
      </View>
    );
  };

  return (
    <View style={[styles.card, styles.scrollCard]} testID="feed">
      <Text style={styles.title} accessibilityRole="header">
        {t("feed.title")}
      </Text>
      {state.kind === "loading" && <Text style={styles.notice}>{t("feed.loading")}</Text>}
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
      {state.kind === "ready" && state.items.length === 0 && <Text style={styles.text}>{t("feed.empty")}</Text>}
      {state.kind === "ready" && state.items.length > 0 && (
        <FlatList
          testID="feed-list"
          style={styles.scrollList}
          contentContainerStyle={styles.scrollContent}
          data={state.items}
          keyExtractor={feedKey}
          renderItem={renderItem}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => {
                setRefreshing(true);
                setReload((n) => n + 1);
              }}
            />
          }
          ListFooterComponent={
            state.hasMore ? (
              <>
                {moreError && <Text style={styles.error} accessibilityRole="alert">{moreError}</Text>}
                <Pressable style={styles.secondary} accessibilityRole="button" disabled={loadingMore} accessibilityState={{ disabled: loadingMore, busy: loadingMore }} onPress={() => void loadMore()}>
                  <Text style={styles.secondaryText}>{loadingMore ? t("feed.loadingMore") : t("feed.more")}</Text>
                </Pressable>
              </>
            ) : undefined
          }
        />
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("communication.welcomeBack")}</Text>
      </Pressable>
    </View>
  );
}
