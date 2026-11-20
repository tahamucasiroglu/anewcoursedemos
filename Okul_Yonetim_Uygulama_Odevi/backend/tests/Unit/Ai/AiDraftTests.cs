using OkulYonetim.Domain.Ai;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Ai;

/// <summary>
/// Spec 0012 AC-1 ve AC-2 kuralları (birim): sesli not taslağı "bekliyor" durumunda, kaynak döküm ve konuşmayla oluşur;
/// model taslağı 1000 karakteri aşarsa 1000'e kısaltılır (1000 olduğu gibi kalır); onay son metin, onaylayan ve mesaj
/// kimliğini yazar ve yalnızca bir kez yapılır — ikinci onay hata verir, ilk onayın bilgileri değişmez.
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
}
