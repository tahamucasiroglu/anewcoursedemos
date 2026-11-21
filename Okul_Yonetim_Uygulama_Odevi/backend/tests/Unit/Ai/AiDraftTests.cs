using OkulYonetim.Domain.Ai;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Ai;

/// <summary>
/// Spec 0012 AC-1 ve AC-2 kuralları (birim): sesli not taslağı "bekliyor" durumunda, kaynak döküm ve konuşmayla oluşur;
/// model taslağı 1000 karakteri aşarsa 1000'e kısaltılır (1000 olduğu gibi kalır); onay son metin, onaylayan ve mesaj
/// kimliğini yazar ve yalnızca bir kez yapılır — ikinci onay hata verir, ilk onayın bilgileri değişmez.
/// Spec 0014 AC-3 (birim): haftalık özet taslağı "bekliyor", dönem Pazartesi, kaynak dayanak; 800 karakteri aşan model metni 800'e
/// kısaltılır; yeniden oluşturma aynı kaydı günceller (tek bekleyen taslak), onaylanmış özette hata verir; onaylayan oluşturandan farklı olabilir.
/// </summary>
public sealed class AiDraftTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void VoiceMessage_New_PendingWithSourceAndContext()
    {
        Guid tenant = Guid.NewGuid();
        Guid teacher = Guid.NewGuid();
        Guid student = Guid.NewGuid();
        Guid conversation = Guid.NewGuid();

        AiDraft draft = AiDraft.VoiceMessage(tenant, teacher, student, conversation, "  ali bugün ödevini getirmedi  ", " Sayın veli, Ali bugün ödevini getirmedi. ", Now);

        draft.Kind.ShouldBe(AiDraftKind.VoiceMessage);
        draft.Status.ShouldBe(AiDraftStatus.Pending);
        draft.IsPending.ShouldBeTrue();
        draft.TenantId.ShouldBe(tenant);
        draft.CreatedByUserId.ShouldBe(teacher);
        draft.StudentId.ShouldBe(student);
        draft.ContextId.ShouldBe(conversation);
        draft.SourceText.ShouldBe("ali bugün ödevini getirmedi");
        draft.DraftText.ShouldBe("Sayın veli, Ali bugün ödevini getirmedi.");
        draft.CreatedAt.ShouldBe(Now);
        draft.FinalText.ShouldBeNull();
        draft.ResultId.ShouldBeNull();
    }

    [Theory]
    [InlineData(999, 999)]
    [InlineData(1000, 1000)]
    [InlineData(1001, 1000)]
    [InlineData(5000, 1000)]
    public void VoiceMessage_LongModelDraft_ShortenedTo1000(int length, int expected)
    {
        AiDraft draft = AiDraft.VoiceMessage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "döküm", new string('a', length), Now);

        draft.DraftText.Length.ShouldBe(expected);
    }

    [Fact]
    public void Approve_Pending_WritesFinalTextApproverAndResult()
    {
        AiDraft draft = AiDraft.VoiceMessage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "döküm", "taslak", Now);
        Guid teacher = draft.CreatedByUserId;
        Guid message = Guid.NewGuid();

        draft.Approve("öğretmenin son metni", teacher, message, Now.AddMinutes(2));

        draft.Status.ShouldBe(AiDraftStatus.Approved);
        draft.IsPending.ShouldBeFalse();
        draft.FinalText.ShouldBe("öğretmenin son metni");
        draft.DraftText.ShouldBe("taslak", "taslak metni kaynak olarak kalır");
        draft.ApprovedByUserId.ShouldBe(teacher);
        draft.ResultId.ShouldBe(message);
        draft.ApprovedAt.ShouldBe(Now.AddMinutes(2));
    }

    [Fact]
    public void Approve_Twice_ThrowsAndKeepsFirstApproval()
    {
        AiDraft draft = AiDraft.VoiceMessage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "döküm", "taslak", Now);
        Guid first = Guid.NewGuid();
        draft.Approve("ilk", draft.CreatedByUserId, first, Now);

        Should.Throw<InvalidOperationException>(() => draft.Approve("ikinci", draft.CreatedByUserId, Guid.NewGuid(), Now.AddMinutes(1)));

        draft.FinalText.ShouldBe("ilk");
        draft.ResultId.ShouldBe(first);
    }

    [Fact]
    public void WeeklySummary_New_PendingWithPeriodAndSource()
    {
        Guid teacher = Guid.NewGuid();
        Guid student = Guid.NewGuid();

        AiDraft draft = AiDraft.WeeklySummary(Guid.NewGuid(), teacher, student, new DateOnly(2026, 11, 16), "{\"dayanak\":1}", "  Ali bu hafta 4 gün okuldaydı.  ", Now);

        draft.Kind.ShouldBe(AiDraftKind.WeeklySummary);
        draft.IsPending.ShouldBeTrue();
        draft.PeriodStart.ShouldBe(new DateOnly(2026, 11, 16));
        draft.ContextId.ShouldBeNull();
        draft.StudentId.ShouldBe(student);
        draft.CreatedByUserId.ShouldBe(teacher);
        draft.SourceText.ShouldBe("{\"dayanak\":1}");
        draft.DraftText.ShouldBe("Ali bu hafta 4 gün okuldaydı.");
        Should.Throw<ArgumentOutOfRangeException>(() => AiDraft.WeeklySummary(Guid.NewGuid(), teacher, student, new DateOnly(2026, 11, 17), "{}", "x", Now));
    }

    [Theory]
    [InlineData(799, 799)]
    [InlineData(800, 800)]
    [InlineData(801, 800)]
    [InlineData(3000, 800)]
    public void WeeklySummary_LongModelText_ShortenedTo800(int length, int expected)
    {
        AiDraft draft = AiDraft.WeeklySummary(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 16), "{}", new string('a', length), Now);

        draft.DraftText.Length.ShouldBe(expected);
        draft.Regenerate(Guid.NewGuid(), "{}", new string('b', length), Now);
        draft.DraftText.Length.ShouldBe(expected);
    }

    [Fact]
    public void Regenerate_Pending_SameRecordNewSourceTextAndCreator()
    {
        AiDraft draft = AiDraft.WeeklySummary(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 16), "eski dayanak", "eski taslak", Now);
        Guid id = draft.Id;
        Guid second = Guid.NewGuid();

        draft.Regenerate(second, "yeni dayanak", "yeni taslak", Now.AddMinutes(5));

        draft.Id.ShouldBe(id);
        draft.IsPending.ShouldBeTrue();
        draft.SourceText.ShouldBe("yeni dayanak");
        draft.DraftText.ShouldBe("yeni taslak");
        draft.CreatedByUserId.ShouldBe(second);
        draft.CreatedAt.ShouldBe(Now.AddMinutes(5));
        draft.PeriodStart.ShouldBe(new DateOnly(2026, 11, 16));
    }

    [Fact]
    public void Regenerate_ApprovedOrVoiceMessage_Throws()
    {
        AiDraft summary = AiDraft.WeeklySummary(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 16), "dayanak", "taslak", Now);
        Guid approver = Guid.NewGuid();
        summary.Approve("son metin", approver, null, Now.AddMinutes(1));

        Should.Throw<InvalidOperationException>(() => summary.Regenerate(Guid.NewGuid(), "x", "y", Now.AddMinutes(2)));
        summary.FinalText.ShouldBe("son metin");
        summary.DraftText.ShouldBe("taslak");
        summary.ApprovedByUserId.ShouldBe(approver, "onaylayan oluşturandan farklı olabilir");
        summary.ResultId.ShouldBeNull();

        AiDraft voice = AiDraft.VoiceMessage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "döküm", "taslak", Now);
        Should.Throw<InvalidOperationException>(() => voice.Regenerate(Guid.NewGuid(), "x", "y", Now));
    }
}
