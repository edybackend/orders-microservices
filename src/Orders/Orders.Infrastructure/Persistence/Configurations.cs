using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain;

namespace Orders.Infrastructure.Persistence;

// Explicit snake_case names keep the schema readable for the Dapper queries on the read side.
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.CustomerEmail).HasColumnName("customer_email").HasMaxLength(254).IsRequired();
        builder.Property(o => o.Total).HasColumnName("total").HasPrecision(18, 2);
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(o => o.CreatedAt);

        builder.OwnsMany(o => o.Items, items =>
        {
            items.ToTable("order_items");
            items.WithOwner().HasForeignKey("order_id");
            items.Property<int>("id");
            items.HasKey("id");
            items.Property(i => i.ProductName).HasColumnName("product_name").HasMaxLength(OrderItem.MaxProductNameLength).IsRequired();
            items.Property(i => i.Quantity).HasColumnName("quantity");
            items.Property(i => i.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        });
        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");
        builder.HasKey(m => m.EventId);
        builder.Property(m => m.EventId).HasColumnName("event_id");
        builder.Property(m => m.Consumer).HasColumnName("consumer").HasMaxLength(100);
        builder.Property(m => m.ProcessedAt).HasColumnName("processed_at");
    }
}
