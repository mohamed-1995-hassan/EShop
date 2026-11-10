using EShop.Domain.Aggregates.IdentityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for User aggregate
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(u => u.TwoFactorSecret)
            .HasMaxLength(500);

        builder.Property(u => u.SecurityStamp)
            .HasMaxLength(100);

        // Relationships
        builder.HasMany(u => u.Roles)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.ExternalLogins)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Claims)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(u => u.DomainEvents);
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex("UserId", "Role")
            .IsUnique();

        builder.Ignore(r => r.DomainEvents);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.Property(t => t.CreatedByIp)
            .HasMaxLength(50);

        builder.Property(t => t.UserAgent)
            .HasMaxLength(500);

        builder.Property(t => t.RevokedByIp)
            .HasMaxLength(50);

        builder.Property(t => t.RevokedReason)
            .HasMaxLength(500);

        builder.Property(t => t.ReplacedByToken)
            .HasMaxLength(200);

        builder.Ignore(t => t.DomainEvents);
    }
}

public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("external_logins");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.ProviderKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex("UserId", "Provider")
            .IsUnique();

        builder.HasIndex(l => new { l.Provider, l.ProviderKey })
            .IsUnique();

        builder.Property(l => l.DisplayName)
            .HasMaxLength(256);

        builder.Property(l => l.FirstName)
            .HasMaxLength(100);

        builder.Property(l => l.LastName)
            .HasMaxLength(100);

        builder.Property(l => l.ProfilePictureUrl)
            .HasMaxLength(1000);

        builder.Ignore(l => l.DomainEvents);
    }
}

public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.ToTable("user_claims");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Value)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Ignore(c => c.DomainEvents);
    }
}

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("email_verification_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.DomainEvents);
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.DomainEvents);
    }
}
