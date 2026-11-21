using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using OkulYonetim.Application;
using OkulYonetim.Application.Features.Ai.VoiceMessages;
using OkulYonetim.Application.Features.Communication.Messages;
using OkulYonetim.Domain;
using OkulYonetim.Infrastructure;
using OkulYonetim.Infrastructure.Email;
using OkulYonetim.WebAPI.Controllers;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace OkulYonetim.Tests.Architecture;

/// <summary>
/// Yasak bağımlılık 6 (<c>docs/architecture.md</c>; spec 0012 BR-5): Ai katmanı veliye hiçbir şey göndermez — Communication'ın
/// komutlarına, bildirim kapısına, konuşma deposuna ve varlıklarına bağımlı olmaz; yalnızca konuşma bağlamını okuyan sorguyu
/// (<see cref="GetTeacherConversationContextQuery"/>) kullanır. Bildirim/e-posta portlarına ve doğrudan HTTP'ye de bağlanmaz
/// (dış servis yalnızca port arkasında). Pozitif kontrol: kural kümeleri dolu ve izinli sorgu gerçekten kullanılıyor.
/// Spec 0012 AC-8 (S2; VERIFY V-1): sesli not yolu — Ai katmanı, Infrastructure'daki Ai bağdaştırıcıları ve
/// <see cref="AiDraftsController"/> — dosya sistemine dokunan System.IO tiplerine (dosya/klasör/yol, dosya akışı ve yazıcıları)
/// bağımlı olmaz; ses yalnızca bellekte. Entegrasyon testinin içerikten doğrulayamadığı "yaz ve hemen sil" yolunu da kapatır.
/// </summary>
public sealed class AiModuleRulesTests
{
    private const string AiNamespaces = @"^OkulYonetim\.(Application\.Features|Domain)\.Ai(\..*)?$";
    private const string CommunicationNamespaces = @"^OkulYonetim\.(Application\.Features|Domain)\.Communication(\..*)?$";

    private const string InfrastructureAiNamespace = @"^OkulYonetim\.Infrastructure\.Ai(\..*)?$";

    private static readonly string[] FileSystemTypes =
    [
        "System.IO.File", "System.IO.FileInfo", "System.IO.FileStream", "System.IO.FileSystemInfo", "System.IO.Directory",
        "System.IO.DirectoryInfo", "System.IO.Path", "System.IO.StreamWriter", "System.IO.BinaryWriter", "System.IO.RandomAccess",
        "System.IO.FileSystemWatcher", "System.IO.MemoryMappedFiles.MemoryMappedFile",
    ];

    private static readonly ArchModel Architecture = new ArchLoader()
        .LoadAssemblies(typeof(DomainAssemblyMarker).Assembly, typeof(ApplicationServiceRegistration).Assembly)
        .Build();

    private static readonly ArchModel VoiceNoteArchitecture = new ArchLoader()
        .LoadAssemblies(
            typeof(DomainAssemblyMarker).Assembly,
            typeof(ApplicationServiceRegistration).Assembly,
            typeof(InfrastructureServiceRegistration).Assembly,
            typeof(AiDraftsController).Assembly)
        .Build();

    [Fact]
    public void Ai_DoesNotDependOnCommunicationExceptContextQuery()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(AiNamespaces)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(CommunicationNamespaces)
                .And().DoNotHaveFullName(typeof(GetTeacherConversationContextQuery).FullName!)
                .And().DoNotHaveFullName(typeof(TeacherConversationContext).FullName!));
        rule.Check(Architecture);
    }

    [Fact]
    public void Ai_DoesNotUseNotificationPortsOrHttp()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(AiNamespaces)
            .Should().NotDependOnAny(Types().That().HaveFullName(typeof(Application.Ports.IPushNotifier).FullName!)
                .Or().HaveFullName(typeof(Application.Ports.IEmailOutbox).FullName!)
                .Or().HaveFullName(typeof(Application.Ports.IEmailSender).FullName!)
                .Or().HaveFullName(typeof(Application.Ports.IConversationRepository).FullName!))
            .AndShould().NotDependOnAny(Types(true).That().ResideInNamespaceMatching(@"^System\.Net\.Http(\..*)?$"));
        rule.Check(Architecture);
    }

    [Fact]
    public void PositiveControl_AiTypesExistAndUseContextQuery()
    {
        Types().That().ResideInNamespaceMatching(AiNamespaces).GetObjects(Architecture).Count().ShouldBeGreaterThan(5);
        Types().That().ResideInNamespaceMatching(CommunicationNamespaces).GetObjects(Architecture).ShouldNotBeEmpty();

        TypesShouldConjunction mustFail = Types().That().HaveFullName(typeof(CreateVoiceMessageDraftCommandHandler).FullName!)
            .Should().NotDependOnAny(Types().That().HaveFullName(typeof(GetTeacherConversationContextQuery).FullName!));
        mustFail.HasNoViolations(Architecture).ShouldBeFalse("Ai sesli not işleyicisi konuşma bağlamı sorgusunu kullanmalı");
    }

    [Fact]
    public void VoiceNotePath_DoesNotWriteFiles()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(AiNamespaces)
            .Or().ResideInNamespaceMatching(InfrastructureAiNamespace)
            .Or().HaveFullName(typeof(AiDraftsController).FullName!)
            .Should().NotDependOnAny(FileSystem());
        rule.Check(VoiceNoteArchitecture);
    }

    [Fact]
    public void PositiveControl_FileSystemRuleCatchesFileWriters()
    {
        Types().That().ResideInNamespaceMatching(InfrastructureAiNamespace).GetObjects(VoiceNoteArchitecture).ShouldNotBeEmpty();
        Types().That().HaveFullName(typeof(AiDraftsController).FullName!).GetObjects(VoiceNoteArchitecture).ShouldHaveSingleItem();

        TypesShouldConjunction mustFail = Types().That().HaveFullName(typeof(EmailSender).FullName!)
            .Should().NotDependOnAny(FileSystem());
        mustFail.HasNoViolations(VoiceNoteArchitecture).ShouldBeFalse("e-posta toplama klasörüne yazan tip kurala takılmalı");
    }

    private static GivenTypesConjunction FileSystem()
    {
        GivenTypesConjunction types = Types(true).That().HaveFullName(FileSystemTypes[0]);
        foreach (string name in FileSystemTypes[1..])
        {
            types = types.Or().HaveFullName(name);
        }

        return types;
    }
}
