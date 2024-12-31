using System.Text.Json;
using Genesis.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Genesis.Core.Content.Database;

internal sealed class DataContext : DbContext
{
    public DataContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Region> Regions => Set<Region>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var DynamicConverter = new ValueConverter<Dictionary<string, Dynamic>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, Dynamic>>(v, (JsonSerializerOptions?)null)!
        );

        var EntityConverter = new ValueConverter<HashSet<Entity>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<HashSet<Entity>>(v, (JsonSerializerOptions?)null)!
        );

        base.OnModelCreating(modelBuilder);

        foreach (var type in modelBuilder.Model.GetEntityTypes())
        {
            if ((!type.ClrType.IsAbstract) && type.ClrType.IsAssignableTo(typeof(Entity)))
            {
                modelBuilder.Entity(type.ClrType).Property(nameof(Entity.Entities)).HasConversion(EntityConverter);
                modelBuilder.Entity(type.ClrType).Property(nameof(Entity.Properties)).HasConversion(DynamicConverter);
            }
        }
    }
}
