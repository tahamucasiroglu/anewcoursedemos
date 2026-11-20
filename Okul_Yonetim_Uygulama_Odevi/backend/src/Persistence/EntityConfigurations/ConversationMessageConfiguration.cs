using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Mesaj tablosu (conversation_messages, spec 0011): konuşmaya ve gönderene bağlı; metin en fazla 2000 karakter. Konuşma +
/// zaman dizini son 50 mesajı ve "Daha eski" sayfasını hızlandırır.
/// </summary>
public sealed class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("conversation_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Body).HasMaxLength(ConversationMessage.BodyMaxLength);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Conversation>().WithMany().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => new { m.ConversationId, m.SentAt });
    }
}
