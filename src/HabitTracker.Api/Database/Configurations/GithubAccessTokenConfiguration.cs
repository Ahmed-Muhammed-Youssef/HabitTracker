using HabitTracker.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HabitTracker.Api.Database.Configurations;

public class GithubAccessTokenConfiguration : IEntityTypeConfiguration<GithubAccessToken>
{
    public void Configure(EntityTypeBuilder<GithubAccessToken> builder)
    {
        builder.HasKey(gat => gat.Id);

        builder.Property(gat => gat.Id).HasMaxLength(500);
        builder.Property(gat => gat.UserId).HasMaxLength(500);
        builder.Property(gat => gat.Token).HasMaxLength(1000);

        builder.HasIndex(gat => gat.UserId).IsUnique();

        builder.HasOne(gat => gat.User)
            .WithOne()
            .HasForeignKey<GithubAccessToken>(gat => gat.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
