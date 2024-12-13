using System.Text.Json;
using Genesis.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Genesis.Core.Content.Database;

internal sealed class EntityContext : DbContext
{
    public EntityContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Region> Regions => Set<Region>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var DynamicConverter = new ValueConverter<Dictionary<string, Dynamic>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, Dynamic>>(v, (JsonSerializerOptions?)null)!
        );

        var EffectsConverter = new ValueConverter<ICollection<Effect>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ICollection<Effect>>(v, (JsonSerializerOptions?)null)!
        );

        var MortalsConverter = new ValueConverter<ICollection<Mortal>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ICollection<Mortal>>(v, (JsonSerializerOptions?)null)!
        );

        var PortalsConverter = new ValueConverter<ICollection<Portal>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ICollection<Portal>>(v, (JsonSerializerOptions?)null)!
        );

        var WidgetsConverter = new ValueConverter<ICollection<Widget>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ICollection<Widget>>(v, (JsonSerializerOptions?)null)!
        );

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Player>().Property(e => e.Effects).HasConversion(EffectsConverter);
        modelBuilder.Entity<Player>().Property(e => e.Widgets).HasConversion(WidgetsConverter);

        modelBuilder.Entity<Region>().Property(e => e.Effects).HasConversion(EffectsConverter);
        modelBuilder.Entity<Region>().Property(e => e.Mortals).HasConversion(MortalsConverter);
        modelBuilder.Entity<Region>().Property(e => e.Portals).HasConversion(PortalsConverter);
        modelBuilder.Entity<Region>().Property(e => e.Widgets).HasConversion(WidgetsConverter);

        foreach (var type in modelBuilder.Model.GetEntityTypes())
        {
            if ((!type.ClrType.IsAbstract) && type.ClrType.IsAssignableTo(typeof(Entity)))
            {
                modelBuilder.Entity(type.ClrType).Property(nameof(Entity.Properties)).HasConversion(DynamicConverter);
            }
        }
    }
}
