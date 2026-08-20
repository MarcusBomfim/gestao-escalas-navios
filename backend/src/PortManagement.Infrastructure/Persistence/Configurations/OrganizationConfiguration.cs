using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.Organizations;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(organization => organization.Id);
        builder.Property(organization => organization.Id).ValueGeneratedNever();
        builder.Property(organization => organization.Name).HasMaxLength(180).IsRequired();
        builder.Property(organization => organization.RegistrationNumber).HasMaxLength(40).IsRequired();
        builder.Property(organization => organization.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.HasIndex(organization => organization.RegistrationNumber).IsUnique();
        builder.HasIndex(organization => new { organization.Type, organization.IsActive });
    }
}
