using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// AI taslağı tablosu (ai_drafts, spec 0012): kaynak (döküm), taslak ve son metin <c>text</c>; ses kolonu yoktur (ses
/// saklanmaz — gereksinim 7). Bağlam (konuşma) ve sonuç (mesaj) kimlikleri yabancı anahtarsızdır: Ai modülü başka modülün
/// tablosuna bağlanmaz; okul, oluşturan kişi ve öğrenci bağlıdır.
/// </summary>
public sealed class AiDraftConfiguration : IEntityTypeConfiguration<AiDraft>
{
    public void Configure(EntityTypeBuilder<AiDraft> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ai_drafts");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(d => d.SourceText).HasColumnType("text");
        builder.Property(d => d.DraftText).HasColumnType("text");
        builder.Property(d => d.FinalText).HasColumnType("text");
        builder.Ignore(d => d.IsPending);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(d => d.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => new { d.TenantId, d.CreatedByUserId, d.CreatedAt });
    }
}
