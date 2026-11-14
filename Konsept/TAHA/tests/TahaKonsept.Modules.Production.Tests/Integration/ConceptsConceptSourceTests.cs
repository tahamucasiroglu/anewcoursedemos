using NSubstitute;
using Shouldly;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Production.Infrastructure.Integration.Concrete;
using TahaKonsept.Shared.Contracts.Concepts;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Integration;

/// <summary>
/// Concepts <c>IConceptReader</c> → Production <c>IConceptSource</c> adaptörü testleri: alan eşleme, silinmiş/bulunmayan konsept ve boş kimlik.
/// </summary>
public sealed class ConceptsConceptSourceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Onaylı konsept kimlik, başlık, profil ve belge ile eşlenir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Maps_approved_concept()
    {
        var conceptId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var document = new ConceptDocument { Title = "Belge başlığı", Pronunciations = [new PronunciationEntry { Written = "Google", Spoken = "Gugıl" }] };
        var reader = Substitute.For<IConceptReader>();
        reader.GetAsync(UserId, conceptId, Arg.Any<CancellationToken>())
            .Returns(new ConceptDetailDto { Id = conceptId, Title = "Fetih", ProfileId = profileId, Status = ConceptStatus.Approved, Document = document });

        var snapshot = await new ConceptsConceptSource(reader).GetAsync(UserId, conceptId, TestContext.Current.CancellationToken);

        snapshot.ShouldNotBeNull();
        snapshot.ConceptId.ShouldBe(conceptId);
        snapshot.Title.ShouldBe("Fetih");
        snapshot.ProfileId.ShouldBe(profileId);
        snapshot.Document.ShouldBeSameAs(document);
    }

    /// <summary>Başlık boşsa belge başlığı kullanılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Falls_back_to_document_title()
    {
        var conceptId = Guid.NewGuid();
        var reader = Substitute.For<IConceptReader>();
        reader.GetAsync(UserId, conceptId, Arg.Any<CancellationToken>())
            .Returns(new ConceptDetailDto { Id = conceptId, Title = " ", Status = ConceptStatus.Approved, Document = new ConceptDocument { Title = "Belge" } });

        var snapshot = await new ConceptsConceptSource(reader).GetAsync(UserId, conceptId, TestContext.Current.CancellationToken);

        snapshot.ShouldNotBeNull();
        snapshot.Title.ShouldBe("Belge");
    }

    /// <summary>Silinmiş ya da bulunmayan konsept için null döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Returns_null_for_deleted_or_missing()
    {
        var deletedId = Guid.NewGuid();
        var softDeletedId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        var reader = Substitute.For<IConceptReader>();
        reader.GetAsync(UserId, deletedId, Arg.Any<CancellationToken>())
            .Returns(new ConceptDetailDto { Id = deletedId, Status = ConceptStatus.Deleted });
        reader.GetAsync(UserId, softDeletedId, Arg.Any<CancellationToken>())
            .Returns(new ConceptDetailDto { Id = softDeletedId, Status = ConceptStatus.Generated, DeletedAt = DateTimeOffset.UtcNow });
        reader.GetAsync(UserId, missingId, Arg.Any<CancellationToken>()).Returns((ConceptDetailDto?)null);
        var source = new ConceptsConceptSource(reader);
        var ct = TestContext.Current.CancellationToken;

        (await source.GetAsync(UserId, deletedId, ct)).ShouldBeNull();
        (await source.GetAsync(UserId, softDeletedId, ct)).ShouldBeNull();
        (await source.GetAsync(UserId, missingId, ct)).ShouldBeNull();
        (await source.GetAsync(UserId, Guid.Empty, ct)).ShouldBeNull();
    }
}
