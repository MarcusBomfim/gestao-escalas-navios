using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.Operations;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class CargoOperationConfiguration : IEntityTypeConfiguration<CargoOperation>
{
    public void Configure(EntityTypeBuilder<CargoOperation> builder)
    {
        builder.ToTable(
            "cargo_operations",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_cargo_operations_planned_quantity",
                    "planned_quantity >= 0");
                tableBuilder.HasCheckConstraint(
                    "ck_cargo_operations_actual_quantity",
                    "actual_quantity IS NULL OR actual_quantity >= 0");
                tableBuilder.HasCheckConstraint(
                    "ck_cargo_operations_dangerous_classification",
                    "NOT is_dangerous_cargo OR dangerous_cargo_classification IS NOT NULL");
            });
        builder.HasKey(operation => operation.Id);
        builder.Property(operation => operation.Id).ValueGeneratedNever();
        builder.Property(operation => operation.Direction).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(operation => operation.CargoType).HasMaxLength(120).IsRequired();
        builder.Property(operation => operation.PlannedQuantity).HasPrecision(18, 3);
        builder.Property(operation => operation.ActualQuantity).HasPrecision(18, 3);
        builder.Property(operation => operation.QuantityUnit).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(operation => operation.DangerousCargoClassification).HasMaxLength(80);
        builder.HasIndex(operation => operation.PortCallId);
        builder.HasOne(operation => operation.PortCall)
            .WithMany()
            .HasForeignKey(operation => operation.PortCallId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
