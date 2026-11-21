import { useEffect, useState } from "react";
import {
  callApi,
  dayAndMonth,
  turkeyDayOf,
  turkeyTimeOf,
  weekdayOf,
  type ApiOutcome,
  type AuthSession,
  type BookableTeacher,
  type MyAppointments,
  type OpenSlot,
  type TeacherSchedule,
} from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

type Client = AuthSession["client"];
type Failure = { unreachable: true } | { unreachable: false; message: string | null };

export type LoadState<T> = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; data: T };

/** Hata metni: sunucunun Türkçe mesajı (alan mesajı önce — T20), yoksa verilen yedek; sunucuya ulaşılamıyorsa ortak metin. */
export function failureText(outcome: Failure, fallback: TextKey): string {
  return outcome.unreachable ? t("appointments.unreachable") : outcome.message ?? t(fallback);
}

/** Anın Türkiye saatiyle "Perşembe 26 Kasım 14:20" metni (dilim etiketi ve başlıklar). */
export function whenText(at: string): string {
  const day = turkeyDayOf(at);
  const { day: date, month } = dayAndMonth(day);
  return `${t(`day.${weekdayOf(day)}` as TextKey)} ${date} ${t(`month.${month}` as TextKey)} ${turkeyTimeOf(at)}`;
}

/** Türkiye gününün başlığı: "Perşembe 26 Kasım". */
export function dayHeaderText(day: string): string {
  const { day: date, month } = dayAndMonth(day);
  return `${t(`day.${weekdayOf(day)}` as TextKey)} ${date} ${t(`month.${month}` as TextKey)}`;
}

/**
 * Bir isteği yükler: yükleniyor / hata + tekrar dene / hazır; `reload` listeyi yeniler (eski ekran — ör. "Bu saat artık dolu.").
 * `enabled` yanlışken istek gitmez (henüz çocuk/öğretmen seçilmedi).
 */
function useLoad<T>(load: () => Promise<ApiOutcome<T>>, deps: readonly unknown[], enabled = true) {
  const [state, setState] = useState<LoadState<T>>({ kind: "loading" });
  const [version, setVersion] = useState(0);

  useEffect(() => {
    if (!enabled) {
      return undefined;
    }

    let active = true;
    void load().then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", data: outcome.data } : { kind: "error", message: failureText(outcome, "appointments.loadError") });
      }
    });
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- yükleyici her çizimde yeniden kurulur; bağımlılıklar açıkça verilir
  }, [...deps, enabled, version]);

  const reload = () => {
    setState({ kind: "loading" });
    setVersion((n) => n + 1);
  };

  return { state, reload };
}

/** Öğretmenin "Takvimim"i (spec 0016 gereksinim 2): `GET api/appointments/schedule`. */
export function useSchedule(client: Client) {
  return useLoad<TeacherSchedule>(() => callApi(client.GET("/api/appointments/schedule")), [client]);
}

/** Velinin "Randevularım"ı (gereksinim 8): `GET api/appointments/mine`. */
export function useMyAppointments(client: Client) {
  return useLoad<MyAppointments>(() => callApi(client.GET("/api/appointments/mine")), [client]);
}

/** Çocuğun öğretmenleri (gereksinim 3): `GET api/appointments/teachers?studentId=`. */
export function useBookableTeachers(client: Client, studentId: string | null) {
  return useLoad<BookableTeacher[]>(
    () => callApi(client.GET("/api/appointments/teachers", { params: { query: { studentId: studentId ?? "" } } })),
    [client, studentId],
    studentId !== null,
  );
}

/** Öğretmenin başlamamış boş dilimleri (gereksinim 3): `GET api/appointments/teachers/{id}/slots?studentId=`. */
export function useOpenSlots(client: Client, teacherId: string | null, studentId: string | null) {
  return useLoad<OpenSlot[]>(
    () => callApi(client.GET("/api/appointments/teachers/{teacherId}/slots", { params: { path: { teacherId: teacherId ?? "" }, query: { studentId: studentId ?? "" } } })),
    [client, teacherId, studentId],
    teacherId !== null && studentId !== null,
  );
}
