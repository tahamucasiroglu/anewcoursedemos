# Architecture

> Agents read this file before planning. Bootstrap 2026-11-19.

## System overview
Okul Yönetim Platformu, **nArchitecture tabanlı modüler monolit** bir .NET 10 Web API, bir React yönetim
paneli ve tek bir Expo mobil uygulamasından (veli + öğretmen) oluşur. Tek veritabanı PostgreSQL 18; kiracı
izolasyonu satır düzeyinde `TenantId`/`BranchId` + EF Core global query filter ile yapılır. Mikroservis yok:
tek ekip, MVP ve kapsam dokümanının "nArchitecture tabanı" kararı (ADR-0001). Dış servisler (ödeme, SMS,
WhatsApp, push, AI, ses→metin) port arayüzlerinin arkasındadır; MVP'de sahte uygulamalar çalışır.

**Stack ve sürümler** (kesin paket sürümleri walking skeleton planında sabitlenir; merkezi `Directory.Packages.props`):

| Katman | Seçim |
|---|---|
| Backend | .NET 10 (LTS), ASP.NET Core Web API, C# 14 |
| Çekirdek | `NArchitecture.Core.*` 2.0.0 (net10.0): Mediator/CQRS, pipeline'lar, repository, dinamik sorgu, güvenlik |
| Veri erişimi | EF Core 10 + Npgsql; snake_case isimlendirme |
| Veritabanı | PostgreSQL 18 (yerel: servis `postgresql-x64-18`, port 5432, locale C / UTF8) |
| Web paneli | React 19 + TypeScript + Vite |
| Mobil | React Native + Expo (TypeScript) |
| Ortak istemci | `packages/api-client` — OpenAPI'den üretilen TypeScript istemcisi |
| Ortak tema | `packages/theme` — açık/koyu renk sözlüğü (`tokens.json`), Sistem/Açık/Koyu seçim mantığı, kontrast hesabı (spec 0002) |

**Klasör düzeni**
```
backend/
  Directory.Build.props · Directory.Packages.props · .editorconfig · OkulYonetim.sln
  src/   Domain · Application · Persistence · Infrastructure · WebAPI
  tests/ Unit · Integration · Architecture
web/ · mobile/ · packages/api-client/ · packages/theme/
```

## Modules / components and ownership
Modüller `Application/Features/<Modül>` altında (Commands, Queries, Rules, Dtos); varlıklar `Domain`'de.

| Module | Single responsibility | Owns |
|---|---|---|
| Organization | Kurum, şube, sınıf seviyesi, sınıf, dönem, akademik takvim (spec 0008), sınıf öğretmeni bağı (spec 0010), ders programı; güncel dönem ve okul günü hesabı (`SchoolCalendar`, Türkiye günü; tatil günü okul günü değildir) | Tenant, Branch, GradeLevel, Classroom, Term, CalendarEntry, ClassroomTeacher, Timetable |
| Students | Öğrenci kartı, veli bağı (kardeşlik bağdan çıkar), sağlık notu/alerji (şifreli), teslim alabilecek kişiler, velinin "Çocuklarım" görünümü (spec 0009); sonra nakil, Excel/e-Okul içe aktarma | Student, StudentGuardian, PickupPerson, Transfer |
| Attendance | Günlük yoklama (spec 0010): öğretmenin "Sınıflarım"ı, bugünkü liste, Var/Yok/Geç kaydı ve düzeltmesi (yalnızca bugün, Türkiye günü); devamsızlık değişikliklerini Communication'a verir | AttendanceSheet, AttendanceEntry |
| Communication | Duyuru, birebir mesaj, onay formu, dosya/fotoğraf paylaşımı, okundu bilgisi, sessiz mod; **veliye giden her şeyin tek çıkışı** (bildirim kapısı); spec 0010: veli devamsızlık bildirimi (uygulama içi + e-posta kuyruğu) ve velinin Bildirimler listesi; spec 0011: duyuru + okundu + onay formu, öğretmen–veli konuşması, mesai saatleri | Announcement, AnnouncementRecipient, AnnouncementStudent, ConsentResponse, Conversation, ConversationMessage, QuietHours, GuardianNotice |
| Academics | Ödev, teslim durumu, yazılı/sözlü not (spec 0013): öğretmenin ödev verme, teslim işaretleme, değerlendirme açma ve puan girişi; velinin çocuk özeti; öğrenci bazlı okuma sorgusu (spec 0014 haftalık özetin kaynağı) | Assignment, AssignmentSubmission, Assessment, Grade |
| Billing | Ücret tarifesi, taksit planı, indirim/burs, tahsilat, cari, makbuz, gecikme hatırlatması (spec 0015: elle tahsilat, en eski vadeden dağıtım, okul başına sıralı makbuz no, öğrenci başına cari, otomatik hatırlatma işçisi) | FeeSchedule, DiscountDefinition, InstallmentPlan, Installment, Payment, PaymentAllocation, ReceiptCounter, PaymentReminder |
| Appointments | Öğretmen müsaitliği (tarihli aralık → görüşme dilimleri), veli randevu talebi, onay/ret/iptal, öğretmenin "Takvimim"i ve velinin "Randevularım"ı (spec 0016); bildirimler Communication kapısından | AvailabilitySlot, Appointment |
| Dashboard | Yönetici günlük özeti, temel raporlar, Excel dışa aktarma (yalnızca okur) | — |
| Ai | Ortak AI yeteneği: taslak üretimi, kaynak gösterme, sağlayıcı portu (sesli not → mesaj taslağı, spec 0014 haftalık gelişim özeti — dayanak toplama, taslak, onay kapısı; gönderim Communication'da) | AiDraft |
| Identity | Kullanıcı, rol ataması, okul üyeliği (etkin/pasif), giriş, 2FA (platform yöneticisi — spec 0007), token | User, RoleAssignment, TenantMembership, TwoFactorChallenge |
| Administration | Kurucunun yönetim işlemleri (spec 0005): şube ekle/düzenle/pasifleştir, kişi ekle, rol ve şube ver/al, pasifleştir, şifre sıfırla; spec 0008: sınıf seviyesi, dönem, sınıf ve akademik takvim yönetimi; spec 0010: sınıf öğretmeni ataması. Organization ve Identity varlıklarını kendi komutlarıyla değiştirir; her işlem okul başına sıralı ve audit'li | — |
| Account | Kişinin kendi hesabı (spec 0006): profil (ad, telefon), şifremi değiştir, şifremi unuttum (yenileme bağlantısı + e-posta). Okul seçimi gerektirmez; yalnızca oturumdaki kişi | PasswordResetToken |
| Platform | Platform yöneticisinin paneli (spec 0007): bütün okulların özet listesi, okul aç (okul açma komutunun kurallarıyla), okulu pasifleştir/etkinleştir. Okulların içine erişmez; tek kurumlar-arası okuma izinli `PlatformDirectory`'de ve yalnızca özet alanlar | — (Tenant.IsActive) |
| Compliance | KVKK rıza kayıtları, audit log, saklama süresi ve anonimleştirme | ConsentRecord, AuditEntry |

"AI ayrı bir modül değil, her modüle gömülü yetenek" (kapsam dokümanı): `Ai` bir ürün modülü değil, diğer
modüllerin ekranlarında kullandığı ortak katmandır.

## Communication rules
- Modül içi: Command/Query → handler → repository.
- Modüller arası okuma: diğer modülün **Query**'si (Mediator) üzerinden; yazma etkisi: diğer modülün **Command**'ı
  veya domain event'i (ör. `StudentMarkedAbsent` → Communication bildirim taslağı/bildirimi).
- Dış dünya: yalnızca `Application`'da tanımlı port arayüzleri (`IPaymentProvider`, `ISmsSender`,
  `IMessagingChannel`, `IPushNotifier`, `IAiTextGenerator`, `ISpeechToText`, `IFileStorage`); uygulamaları `Infrastructure`'da.
- İstemciler (web, mobil): yalnızca HTTP API, yalnızca `packages/api-client` üzerinden.
- Oturum (spec 0004): istemciler oturumu `packages/api-client`'taki `AuthSession` ile yürütür (giriş, okul seçimi, 401'de
  tek yenileme, çıkış); web ve mobil aynı akış durumunu (`authFlow`) çizer, yalnızca yenileme anahtarının taşınması farklı
  (web: HttpOnly çerez, mobil: güvenli depo). Geliştirmede web `/api`'yi vite vekiliyle yerel API'ye iletir (Pi'deki
  gibi aynı adres). Kurum/şube duvarı Persistence'ta global query filter'dır ve oturumdan (`ICurrentSession`) beslenir;
  filtreyi delen okuma (`IgnoreQueryFilters`) yalnızca izinli üç depoda: rol ataması, kurum ve platform paneli özeti
  (`PlatformDirectory`, spec 0007) — architecture testi denetler. Pasif okulun (spec 0007) atamaları girişte, okul
  seçiminde, oturum doğrulamasında ve yenilemede okunmaz.
- Akademik yapı (spec 0008): seviye, dönem ve takvim kaydı kurum filtresinden geçer (takvimde silinen kayıt da elenir);
  sınıf filtresi kendi şube kuralını yazmaz, şube filtresini iç sorguyla miras alır (`Branches.Any(b => b.Id == c.BranchId)`):
  kurucu okulun bütün sınıflarını, diğer roller yalnızca atandıkları **etkin** şubelerin sınıflarını görür. Yönetim uçları
  `api/admin/grade-levels`, `api/admin/terms` (+ `/{id}/calendar-entries`), `api/admin/calendar-entries`,
  `api/admin/classrooms` (`TenantFounder`, okul başına sıra + audit); her rol için `api/calendar` (güncel/yaklaşan dönem ve
  kayıtları) ve `api/classrooms` (görünür etkin sınıflar — öğrenci ve yoklama spec'lerinin okuma ucu). Güncel dönem
  `SchoolCalendar`'da Türkiye gününe göre hesaplanır (`Europe/Istanbul`, bulunamazsa sabit UTC+3).
- Öğrenci ve veli (spec 0009): öğrenci filtresi sınıf gibi şubenin görünürlüğünü miras alır (`Branches.Any(b => b.Id == s.BranchId)`);
  veli bağı ve teslim kişisi kurum filtreli. Kurucu uçları `api/admin/students` (`TenantFounder`, okul başına sıra + audit; kart
  okunuşu da audit'li), veli uçları `api/my/children` (seçili okulda veli rolü; öğrenci **yalnızca oturumdaki kişinin bağı**
  üzerinden okunur — veli rolü şube düzeyinde olduğu için şube filtresi tek başına yetmez). **İstisna (modüller arası
  yazma, plan 0009 Risks 3):** veli bağlama Identity varlıklarını (kişi, üyelik, rol) yazar; Mediator komutu kendi
  işlemini açtığından iç içe komut/kilit kurulamaz. Bu yüzden kişi açma + üyelik + rol çekirdeği Administration'da kilitsiz
  `MemberAccounts` hizmetidir; kişi yönetimi (0005) ve veli bağlama aynı çekirdeği okul + kişi sırası (`AdminGuard.LockAsync`)
  içinde, tek kayıtta kullanır. Sağlık notu ve alerji `IPersonalDataProtector` (AES-GCM, ayrı ortam anahtarı
  `PersonalData:EncryptionKey`) ile şifreli; düz metin yalnızca `StudentHealth` ile istek/yanıt arasında dolaşır.
- Yoklama ve veli bildirimi (spec 0010): kurucu `api/admin/classrooms/{id}/teachers` ile sınıfa, sınıfın şubesinde etkin
  Öğretmen olan kişileri atar (`ClassroomTeacher`, okul başına sıra + audit). Öğretmen uçları `api/attendance`
  (`classrooms`, `classrooms/{id}/today` GET/PUT): her istekte veritabanından Öğretmen rolü (yoksa 403), sınıfa atama ve
  sınıfın şubesinde öğretmenlik (yoksa 404) denetlenir — `AttendanceGuard`. "Bugün" sunucunun Türkiye günüdür
  (`SchoolCalendar`); istemcinin günü yalnızca doğrulanır; dönem dışı ve takvimde tatil olan gün okul günü değildir. Kayıt
  sınıf + gün başına sıradadır (`attendance:<sınıf>:<gün>`); yoklama ve veli bildirimleri aynı işlemde yazılır. İyimser
  eşzamanlılık: liste yoklamanın sürümünü (`version`, yoksa 0) verir, kayıt onu geri gönderir; sıra içinde sürüm eskiyse ve
  kayıt bir durumu değiştirecekse 409, hiçbir şey yazılmaz, istemci listeyi yeniler (review F-1). Attendance
  veliye doğrudan yazmaz: "yok" durumu değişen öğrencileri Communication'ın `NotifyAttendanceChangesCommand`'ına verir
  (veliye giden tek çıkış; spec 0011 duyuru/mesajı bu modülde genişletir). Bildirim veli + öğrenci + gün tekildir
  (`GuardianNotice`: `Absent` / `Corrected`); e-postalar commit'ten sonra `IEmailOutbox`'a bırakılır. Modüller arası okumalar
  Mediator sorgularıyla: sınıf `GetClassroomByIdQuery`, okul günü `GetSchoolDayQuery`, sınıf listesi
  `ListClassroomRosterQuery`, etkin veliler `ListGuardianContactsQuery`, velinin çocukları `ListMyChildrenQuery`. Veli uçları
  `api/notices` (GET, POST `read`): bildirimler yalnızca kişinin **bağlı** çocuklarınınkiler (bütün şubeler); okunmamış sayısı
  `/api/me`'de (`unreadNoticeCount`). Audit kaydı kısa bir `Detail` (yalnızca kimlik ve sayı) taşıyabilir.
- Veli iletişimi (spec 0011): **bildirim kapısı** — veliye (ve mesai içindeyse öğretmene) giden her bildirim Communication'ın
  `GuardianNotifier`'ından, kayıt commit edildikten **sonra** çıkar: bildirimler `IPushNotifier` portuna (MVP'de gerçek push
  yok — `LoggingPushNotifier` yalnızca tür ve kimlikleri loglar; B-6), e-postalar `IEmailOutbox` kuyruğuna; gönderici hatası
  yalnızca kimliklerle Warning loglanır, asıl işlem başarılı kalır. Diğer modüller iki adımla kullanır: işlem içinde kendi
  Communication komutu (yoklama: `NotifyAttendanceChangesCommand` → `GuardianNotice`), commit sonrası
  `DeliverNotificationsCommand` (B-30 ile yoklamanın e-posta adımı da buraya taşındı). Uçlar `api/announcements` (GET kurucu/
  öğretmen listesi, POST yayın, GET `feed` velinin akışı — duyurular + "Okul bildirimi" olarak `GuardianNotice`'ler, 20'lik
  sayfa; GET `{id}` ayrıntı — alıcı velinin ilk açışı okundu; GET `{id}/results`; POST `{id}/consent`), `api/conversations`
  (GET, GET `contacts`, POST başlat, GET/POST `{id}/messages`) ve `api/me/quiet-hours` (GET, PUT). Rol ve ilişki her istekte
  `CommunicationGuard`'da: öğretmen–sınıf ilişkisi yoklamayla aynı sınıf öğretmeni bağıdır; veli–öğrenci bağ üzerinden;
  rol yoksa 403, ilişki yoksa 404. Yeni tablolar yalnızca kurum filtreli (velinin çocukları farklı şubelerde olabilir — BR-2);
  şube/ilişki duvarı guard'da, filtre delme yok. Alıcılar yayın anında yazılır (`AnnouncementRecipient`, veli başına bir
  satır); onay cevabı öğrenci başına tek satır, duyuru + öğrenci sırasında (`consent:<duyuru>:<öğrenci>`). Sessiz mod
  `QuietHours.IsWorkingTime` ile Türkiye saatinde (`SchoolCalendar.TurkeyTime`). Okunmamış duyuru ve mesaj sayıları
  `/api/me`'de (`unreadAnnouncementCount`, `unreadMessageCount`). Modüller arası okumalar: `ListStudentBriefsQuery` (kapsam
  öğrencileri), `ListGuardianContactsQuery`, `ListClassroomRosterQuery`, `ListMyChildrenQuery`, `GetBranchByIdQuery`,
  `GetClassroomByIdQuery`.
- Yerel yapay zekâ (spec 0012): `Ai` ortak katmanı iki port kullanır — `IAiTextGenerator` (talimat + içerik → metin) ve
  `ISpeechToText` (ses → döküm + süre). Uygulamaları `Infrastructure/Ai`: `OllamaTextGenerator` (`POST api/chat`, akışsız,
  düşük sıcaklık, `keep_alive`) ve `WhisperServiceSpeechToText` (`POST transcribe`, ham ses) — bu bilgisayardaki Ollama
  (`gemma3:4b`) ve `tools/ai/stt_server.py` (faster-whisper `medium`, yalnızca `127.0.0.1:8765`, elle `start-stt.ps1` ile
  başlar; varsayılan CPU int8 — 6 GB kartı Ollama'ya bırakır). Adresler `Ai:*` ayarından; boş ya da yerel olmayan adres →
  `Unavailable*` uygulaması (Pi varsayılanı: özellik "Yapay zekâ şu an kullanılamıyor", mesajlaşma çalışır). `AiWorkGate`
  tek ekran kartı için aynı anda tek model çağrısı çalıştırır; sırada 30 sn'den uzun bekleyen "kullanılamıyor" alır.
  Etkileşimli AI çağrısında **yeniden deneme yoktur** (aşağıdaki port kuralının istisnası — kullanıcı bekler): tek deneme,
  ses 120 sn / metin 90 sn zaman aşımı (ses servisi süreyi dökümden önce ölçer, 60 sn üstünü dökmeden boş metin + süreyle
  döner), hata hemen "kullanılamıyor" + Warning log. Akış: mobil konuşma ekranı →
  `POST api/ai/voice-message-drafts` (multipart, ≤ 2 MB, istek ~2,1 MB) → Communication'ın `GetTeacherConversationContextQuery`'si
  (taraf/öğretmen/ilişki) → ses servisi → (≤ 60 sn, boş değil) → metin servisi (yalnızca sabit talimat
  `Features/Ai/Prompts/voice-message.tr.txt` + döküm) → 1000'e kısaltılmış taslak `AiDraft` (bekliyor, kaynak = döküm).
  Gönderim 0011'in `POST api/conversations/{id}/messages` ucundan `aiDraftId` ile: Communication, Ai'ın onay kapısını
  (`GetPendingAiDraftQuery` → mesaj → `MarkAiDraftApprovedCommand`) taslak sırası (`ai-draft:<id>`) içinde, mesajla aynı
  kayıtta çağırır; Ai Communication'a yazmaz (yasak bağımlılık 6). "Vazgeç": `DELETE api/ai/drafts/{id}` taslağı siler.
- Ödev ve not (spec 0013): öğretmen uçları `api/academics` (`classrooms/{id}/assignments` GET/POST, `assignments/{id}/submissions`
  GET/PUT, `classrooms/{id}/assessments` GET/POST, `assessments/{id}/grades` GET/PUT); yetki her istekte `AcademicsGuard`'da —
  öğretmen–sınıf kuralı yoklamayla **tek kaynaktan**: `Organization/TeacherClassroomAccess` (Öğretmen rolü yoksa 403; sınıf
  görünmüyor/pasif/atanmamış/şubesinde artık öğretmen değil 404; `AttendanceGuard` da bunu kullanır). Teslim durumunu ve puanı
  yalnızca kaydı oluşturan öğretmen değiştirir (403). Ödev ve değerlendirme kurum + şube filtreli (yoklama gibi), teslim ve puan
  satırı kurum filtreli; filtre delme yok. Ödevin/değerlendirmenin öğrencileri oluşturulduğu andaki sınıf listesidir
  (`ListClassroomRosterQuery`); listeler yalnızca sınıfta süren öğrencileri gösterir ve sayar, ayrılanın satırı kalır. Kayıtlar
  ödev/değerlendirme başına sıradadır (`assignment:<id>`, `assessment:<id>`). Değerlendirme günü sınıfın dönemi içinde
  (`GetTermByIdQuery`). Yeni ödev bildirimi: işlem içinde Communication'ın `NotifyStudentGuardiansCommand`'ı (kayda bağlı "Okul
  bildirimi" — `GuardianNotice` türü `Assignment`, kaynak kayıt + hazır metin; yoklamanın veli + öğrenci + gün tekilliği yalnızca
  kaynaksız satırlar için), commit sonrası `DeliverNotificationsCommand`. Velinin özeti `api/my/children/{studentId}/academics`
  (veli bağı "Çocuklarım" sorgusundan; yoksa 404). **Öğrenci bazlı okuma** `GetStudentAcademicRecordQuery(öğrenci, başlangıç, bitiş)`
  → ödevler (ders, başlık, teslim, durum) ve notlar (ders, tür, başlık, gün, puan): uç noktası yoktur, **çağıranın erişimini
  denetlemez** (yalnızca kurum + şube filtresi) — erişim denetimi çağırandadır (veli özeti: bağ; spec 0014: kendi kuralı).
- Haftalık gelişim özeti (spec 0014): öğretmen uçları `api/weekly-summaries` (`classrooms` GET, `?classroomId=&week=` GET, `students/{id}`
  GET panel, `students/{id}/draft` POST üretim / DELETE vazgeç, `students/{id}/send` POST gönderim; `week=current|previous`). Hafta Türkiye
  günüyle Pazartesi–Pazar (`Domain/Ai/SchoolWeek`, bugün `SchoolCalendar.Today`). Yetki her istekte `WeeklySummaryGuard`'da, öğretmen–sınıf
  kuralı yine `TeacherClassroomAccess`: Öğretmen rolü yoksa 403; öğrenci görünmüyor / ayrılmış / sınıfsız ya da öğretmen öğrencinin bugünkü
  sınıfına atanmamış 404. **Akış:** Ai dayanağı `WeeklySummarySourceCollector` ile toplar — Attendance `GetStudentWeekAttendanceQuery`
  (yeni, öğrenci bazlı, uçsuz) + Academics `GetStudentAcademicRecordQuery` + Students `ListStudentBriefsQuery` (yalnızca ad); iki öğrenci bazlı
  sorgu da erişim denetlemez ve okul + şube görünürlüğü olan oturum ister (oturumsuz çağrı boş sonuç alır — plan 0014 Risks 9), bu yüzden
  toplayıcıyı yalnızca guard'dan geçmiş işleyiciler çağırır. Dayanak boşsa (yoklama, ödev/not, öğretmen notu yok) model çağrılmaz; değilse
  `IAiTextGenerator`'a yalnızca `Features/Ai/Prompts/weekly-summary.tr.txt` + dayanak metni gider (ad, soyad değil) → 800'e kısaltılmış taslak
  `AiDraft` (tür `WeeklySummary`, `period_start` = haftanın Pazartesi'si, kaynak = dayanak JSON'u; okul + tür + öğrenci + hafta tekil dizin),
  öğrenci-hafta sırası (`weekly-summary:<öğrenci>:<hafta>`) içinde eklenir ya da bekleyen taslak yeniden oluşturulur. Gönderim
  **Communication**'ın `SendWeeklySummaryCommand`'ı: aynı sıra içinde Ai'ın `GetWeeklySummaryForSendQuery`'si (guard + bekleyen taslak) →
  `NotifyStudentGuardiansCommand` (kayda bağlı okul bildirimi `GuardianNotice` türü `WeeklySummary`, metin = onaylı özet + etiket + dayanak
  satırı, ≤ 2000) → Ai'ın `ApproveWeeklySummaryDraftCommand`'ı (onaylayan = oturumdaki öğretmen; sınıfın atanmış her öğretmeni onaylayabilir)
  → audit `WeeklySummarySent` → commit → `DeliverNotificationsCommand`. Ai veliye yazmaz (yasak bağımlılık 6). Velinin akışında satır türü
  `WeeklySummary`, başlık türden ve haftadan ("Haftalık gelişim özeti — 16–22 Kasım"); ayrıntı `GET api/notices/{id}` (yalnızca kendi
  bağlı çocuğunun bildirimi, açılış okundu yapar). Web'de `#/haftalik-ozet`; bağlantı `/api/me` → `teachesClassroom` (Organization
  `ListMyTeachingClassroomsQuery`) doğruysa.
- Tahsilat ve ön muhasebe (spec 0015): personel uçları `api/billing` (`tariffs`, `discounts` GET/POST/PUT + `{id}/activate|deactivate`;
  `students` GET cari listesi — şube, "yalnızca gecikenler", ad araması, 25'lik sayfa, alt toplamlar —, `students/{id}` GET öğrenci carisi;
  `plans/preview` POST, `plans` POST, `plans/{id}/cancel` POST; `payments` POST, `payments/{id}/cancel` POST, `payments/{id}/receipt` GET),
  veli uçları `api/my/payments` (GET) ve `api/my/payments/{studentId}` (GET; bağ yoksa 404). Yetki her istekte `BillingGuard`'da, rol ve
  şube veritabanından: kurucu ya da en az bir şubede Muhasebe (değilse 403; öğretmen, veli); tarife/indirim yazma yalnızca kurucu (403);
  muhasebe yalnızca **Muhasebe rolü olduğu** şubelerin öğrencisini/planını/tahsilatını görür (`BillingStaff.CanSee` — oturumun görünür
  şubeleri bütün rollerin şubeleri olduğu için yetmez, ör. başka şubede öğretmen olan muhasebe), başka okul ya da şube 404. Filtreler:
  tarife, indirim, makbuz sayacı, hatırlatma kurum filtreli; plan ve tahsilat şube görünürlüğünü miras alır; taksit planından, dağılım
  tahsilatından miras alır — filtre delme yok. Para `decimal`/`numeric(18,2)` (DbContext convention, T4) ve her tabloda `CHECK`
  (tutar > 0, 0 ≤ ödenen ≤ tutar, net = tarife − indirim, yüzde 1–100, taksit 1–12, hatırlatma sırası 1–3). Tutar istekte ham metin
  olarak alınır (`JsonValueText`; "abc", `1.005` Türkçe alan hatası) ve istemciler tutarı metin gönderir ("1234.50" — kayan nokta yok).
  Sıralar: katalog `billing-catalog:<okul>` (etkin ad tekilliği), plan açma `billing-student:<öğrenci>` (aynı tarifeden ikinci geçerli
  plan; filtreli tekil dizin ikinci katman), tahsilat `billing-plan:<plan>` + `billing-receipt:<okul>` (kalan borç ve sayaç sıra içinde
  taze okunur; okul + makbuz no tekil dizini ikinci katman), plan/tahsilat iptali `billing-plan:<plan>`. Dağıtım saf `PaymentAllocator`'da
  (vadesi en eski ödenmemiş taksitten); gecikme tanımı yalnızca `Installment.StateOn` (Türkiye günü; vade gününün ertesi günü 00:00'dan
  itibaren) — yönetici paneli (0017) `BillingLedger` yardımcısını kullanır. Öğrenci etiketi (ad, şube, sınıf, ayrılmış mı) Students'ın
  yeni `ListStudentLabelsQuery`'sinden; velinin çocukları `ListMyChildrenQuery`'den; okul adı (makbuz) `ITenantRepository.GetCurrentAsync`.
  **Hatırlatma işçisi** (`Infrastructure/Billing/OverdueReminderWorker`, `BackgroundService`, açılıştan 1 dk sonra, sonra 15 dk'da bir;
  `Billing:RemindersEnabled`, varsayılan açık, testlerde kapalı): etkin okul kimliklerini izinli `IPlatformDirectory.ListActiveTenantIdsAsync`'ten
  alır, her okul için yeni DI kapsamında `OverdueReminderRunner`'ı çağırır; o kapsamın oturumu `CurrentSession.AuthenticateSystem` ile okul
  için **sistem oturumu** olur (kişi yok, okul seçili, bütün şubeler — filtreler okulla sınırlı kalır; yöntem `internal`, yalnızca bu
  çalıştırıcıda — architecture testi `SystemSessionUsageTests`). `SendOverdueRemindersCommand` okul sırasında (`billing-reminders:<okul>`)
  Türkiye saatiyle 10:00'dan sonra vadesi geçmiş ve kalanı olan taksitler için `ReminderSchedule.Due` (1./8./15. gün, en fazla 3, kaçırılan
  günler birikmez) → önce tekil `PaymentReminder` (taksit + sıra), aynı işlemde Communication'ın `NotifyStudentGuardiansCommand`'ı
  (`GuardianNotice` türü `PaymentReminder`, velinin akışında "Okul bildirimi"), commit sonrası `DeliverNotificationsCommand`; bir okulun
  hatası yalnızca okul kimliğiyle Error loglanır, diğerleri sürer. Web'de `#/tahsilat` (+ `#/tahsilat/ogrenci/<id>`) ve `#/makbuz/<id>`
  (yazdırılabilir; `@media print` menü/düğmeleri basmaz); mobilde velide "Ödemeler".
- Randevu (spec 0016): öğretmen uçları `api/appointments` (`schedule` GET — bugünden (Türkiye günü 00:00) itibaren dilimler ve onay bekleyenler,
  `availability` POST, `slots/{id}` DELETE, `{id}/respond` POST), veli uçları (`teachers?studentId=` GET, `teachers/{id}/slots?studentId=` GET, POST talep,
  `mine` GET — bağlı çocukların ve kendi talep ettiği randevular, yaklaşan + son 30 gün), ortak `{id}/cancel` POST. Yetki her istekte `AppointmentsGuard`'da
  (rol yoksa 403, ilişki yoksa 404; çocuğun öteki velisinin talebini iptal 403): veli–öğrenci "Çocuklarım" sorgusundan, öğretmen–sınıf `CommunicationGuard`
  ile aynı kaynaklardan (`ListStudentBriefsQuery`, `GetClassroomByIdQuery`, `IClassroomTeacherRepository`, `ListGuardianContactsQuery`). Dilim ve randevu
  yalnızca kurum filtreli (dilimde ayrıca kaldırılmamış); filtre delme yok. Türkiye saatinden UTC'ye çeviri yalnızca `AvailabilityPlanner`'da
  (`SchoolCalendar.Zone`, bulunamazsa sabit UTC+3); "Süresi doldu" saklanmaz, okumada hesaplanır (zamanlanmış iş yok). Randevu dilimin başlangıç/bitişini
  kopyalar. Bütün değişiklikler öğretmen başına tek sırada (`appointments:teacher:<id>`; tek anahtar — kilit sırası sorunu yok): çakışma, "dilim boş mu",
  "çocuk + öğretmen için açık randevu var mı", "hâlâ onay bekliyor mu" sıra içinde taze okunur; kısmi tekil dizin `appointments(slot_id) WHERE status IN (1, 2)`
  ikinci katman (ihlal "Bu saat artık dolu."). Bildirimler: veliye (onay, ret, öğretmen iptali) işlem içinde Communication'ın `NotifyStudentGuardiansCommand`'ı
  (`GuardianNotice` türü `Appointment`, kaynak randevu ya da öğretmen iptalinde kapanan dilim), commit sonrası `DeliverNotificationsCommand`; öğretmene
  (yeni talep, veli iptali) commit sonrası Communication'ın yeni `NotifyTeacherCommand`'ı — mesai saatleri (`QuietHours`) kapıda uygulanır, metin
  `PushNotification.Text`'te (tür + gün + saat; metin yalnızca `Appointment` türünde kurulabilir). Commit sonrası iki adım (`AppointmentNotifications.
  DeliverToGuardiansAsync`, `ToTeacherAsync`) hatayı yakalar (iptal hariç): Warning + "bildirim gitmedi", istek düşmez. Bekleyen talep sayısı `/api/me`'de (`pendingAppointmentCount`, Appointments'ın
  `CountMyPendingAppointmentsQuery`'si). Mobilde öğretmende "Randevular (n bekleyen)" → Takvimim, velide "Randevular" → Randevularım; web ekranı yok.
- Platform paneli (spec 0007): `api/platform/schools` uçları `PlatformAdmin` politikasıyla korunur — yetki her istekte
  veritabanından ve oturum iki adımlı kodla açılmış olmalı. Platform yöneticisinin şifreli girişi oturum değil 5 dk'lık
  bekleme döner; oturum `api/auth/two-factor/*` uçlarında kodla açılır. Yetki yalnızca komutla verilir (`grant-platform-admin`).
- Yönetim (spec 0005): `api/admin/*` uçları `TenantFounder` politikasıyla korunur (kurucu bilgisi her istekte
  veritabanından); rol/üyelik/şube değiştiren her komut okul başına veritabanı kilidiyle sıraya girer. Web'de yönetim
  sayfası bağımlılıksız hash yönlendirmesiyle `#/yonetim` adresindedir; mobilde yönetim yok. Kişi araması ve şube adı
  karşılaştırması Türkçe harf kuralıyla uygulamada yapılır (okul başına kayıt sayısı küçük).
- E-posta (spec 0006): uygulama e-postayı `IEmailOutbox` kuyruğuna bırakıp hemen döner; arka plan gönderici
  `IEmailSender` ile yollar (SMTP ayarlıysa sunucuya, yalnızca geliştirmede klasöre, değilse "SMTP ayarlı değil" logu).
  Kuyruk bellektedir; uygulama yeniden başlarsa bekleyen e-posta kaybolur (kişi yeniden ister). Bağlantı adresleri
  `IAppLinks` (web adresi ayardan); yenileme anahtarı adresin `#` sonrasında, sunucuya ve loglara gitmez.
- Renkler (web, mobil): yalnızca `packages/theme` (`tokens.json`). Mobil doğrudan kullanır; web aynı değerleri
  `web/src/theme/theme.css` değişkenlerinde tutar (test birebir eşitliği doğrular). Tema seçimi yalnızca cihazda saklanır.
- Port çağrıları zaman aşımı + yeniden deneme ile yapılır; başarısızlık "beklemede" durumuna düşer, isteği çökertmez.
  İstisna (spec 0012): etkileşimli AI çağrıları tek denemedir; hata kullanıcıya hemen "kullanılamıyor" olarak döner.

## Logging (ADR-0003)
- **Kapsamlı loglama** ilk spec'ten itibaren: Serilog (nArchitecture'ın log altyapısı), `ILogger<T>` ile.
- **Günlük dosyalar:** `logs/okul-api-YYYYMMDD.log` — her gün yeni dosya; tek dosya 100 MB'ı aşarsa aynı gün içinde
  bölünür; **30 gün** saklanır, eskisi otomatik silinir. Klasör yolu ayardan; `logs/` repoya girmez.
- **Seviye ayarlanabilir:** ayar dosyası / ortam değişkeni (`Serilog__MinimumLevel__Default`); uygulama yeniden
  derlenmeden değişir. Varsayılan **Information ve üstü**.
- **Biçim:** insanın okuyacağı tek satır: zaman (yerel saat + ofset), seviye, `traceId`, kaynak sınıf, mesaj,
  hata varsa exception. Geliştirmede konsola da yazılır.
- **Ne loglanır:** her HTTP isteği (yöntem, yol, durum kodu, süre, `traceId`, kiracı/kullanıcı **kimliği**);
  her Command'ın başlangıç/bitişi (nArchitecture logging pipeline); iş olayları (Information); iş kuralı ihlali
  ve dış servis aksaklığı (Warning); her beklenmeyen hata, bir kez, `traceId` ile (Error). Kullanıcıya gösterilen
  `traceId` log satırını bulmayı sağlar.
- **Ne loglanmaz:** istek/yanıt gövdesi, şifre, token, TC kimlik no, sağlık verisi, telefon/e-posta/adres
  (maskelenir) — `docs/security.md`.
- Web/mobil istemci hataları MVP'de yalnızca geliştirici konsolunda; sunucuya toplanması backlog'da.

## Runtime environments (ADR-0004)
- **Geliştirme:** Windows makine — API, web (Vite), PostgreSQL 18 yerel servis.
- **MVP deneme ortamı: Raspberry Pi** (yerel ağda, uzaktan erişim). **API ve web orada çalışır**; kısa süreli
  denemeler için kurulup kaldırılır. Bu yüzden: API `linux-arm64` için yayımlanabilir olmalı; tüm adres/port/
  bağlantı/log yolu ayarları ortam değişkeniyle verilir (koda veya Windows yoluna bağlı değil); web statik
  dosya olarak sunulur. Pi kurulumu ve dağıtımı spec 0003 ile geldi (`docs/deploy-pi.md`): web nginx ile ayrı birim
  (port 8080, `/api` içeride API'ye iletilir), API systemd servisi (`okul` kullanıcısı, yalnızca `127.0.0.1:5080`),
  Pi'deki PostgreSQL 17, kısıtlı veritabanı kullanıcısı `okul_app`; dağıtım/geri alma `tools/pi/`.
- Mobil uygulama, Pi'deki API'ye yerel ağ adresiyle bağlanır (adres ortam ayarından).
- Yerel AI (spec 0012) yalnızca Windows geliştirme bilgisayarında (RTX 3060 6 GB) çalışır; Pi'de model yok. Pi'deki API
  varsayılan olarak AI'sız ("kullanılamıyor"); isteğe bağlı demo ayarıyla aynı yerel ağdaki bilgisayarın Ollama ve ses
  servisine yönlendirilir (`docs/deploy-pi.md`, `tools/ai/README.md`).

## Forbidden dependencies (make them testable)
`tests/Architecture` (ArchUnitNET) ve derleme kuralları bunları zorlar; hepsi `scripts/check` içinde.

1. `Domain` hiçbir projeye bağımlı olmaz; paket olarak yalnızca nArchitecture'ın temel varlık paketine
   (`NArchitecture.Core.Persistence`, `Entity<TId>`) referans verir. Domain **kodu** EF Core tiplerini kullanamaz
   (mimari test tip düzeyinde denetler) — ADR-0005.
2. `Application` → `Persistence` / `Infrastructure` / `WebAPI` bağımlılığı yasak; yalnızca kendi arayüzlerini kullanır.
3. `WebAPI` katmanı (controller'lar dahil) `OkulDbContext`'e, EF Core'a ve Npgsql'e bağımlı olmaz; controller'lar iş mantığı
   içermez, yalnızca Command/Query gönderir. Şema geçişi Persistence'taki `MigrateOkulDatabase()` ile çağrılır.
4. Bir modül başka bir modülün repository'sine, varlık konfigürasyonuna veya tablosuna doğrudan erişmez.
5. Dış servis SDK'ları (ödeme, SMS, WhatsApp, push, AI) yalnızca `Infrastructure`'da referans edilir.
6. `Ai` modülü veliye hiçbir şey göndermez (`Communication` portlarına bağımlı olmaz); yalnızca `AiDraft` üretir.
7. `web/` ve `mobile/` backend koduna veya veritabanına erişmez; HTTP'yi yalnızca `packages/api-client` ile kullanır.
8. **K1** Her `.cs` dosyasında tek tip; dosya adı = tip adı (StyleCop SA1402, SA1649 — error).
9. **K2** Uyarılar hatadır, tüm projelerde (`Directory.Build.props`: `TreatWarningsAsErrors`).
10. **K3** Her tipin başında kısa Türkçe `/// <summary>`; üyeler için ayrı summary yazılmaz (architecture testi XML belgesinden doğrular).
11. `IgnoreQueryFilters()` / ham SQL yalnızca izinli listedeki yerlerde (architecture testi; T5).
12. `web/` ve `mobile/` kodunda renk değeri (`#…`, `rgb(…)`, `hsl(…)`) yazılmaz; renk yalnızca ortak temadan (ESLint
    `no-restricted-syntax`; web CSS'i için `cssVariables.test.ts`).

## Deliberately out of scope
- Mikroservisler, ayrı veritabanları.
- Faz 2/3 modülleri (ölçme-değerlendirme, etüt, servis, yemek/sağlık, öğrenci uygulaması, rehberlik, personel,
  ön kayıt CRM, çok şubeli konsolide yönetim, markalı uygulama, pazaryeri) — kapılar geçilene kadar.
- Gerçek ödeme/SMS/WhatsApp entegrasyonu (portlar hazır; sağlayıcı yok — `specs/backlog.md`).
- e-Fatura/e-Arşiv, Google/Microsoft SSO, WhatsApp Business (Faz 2).
- Tam LMS / canlı ders, tam muhasebe / bordro, MEB süreçlerinin yerine geçmek, devlet okulları, veliler arası grup sohbeti (kapsam dokümanı "Kapsam dışı").
- Çoklu dil: MVP yalnızca Türkçe (metinler yine de YAML / `tr.json` dosyalarında).
- PostgreSQL Row-Level Security (ikinci savunma hattı — backlog).
