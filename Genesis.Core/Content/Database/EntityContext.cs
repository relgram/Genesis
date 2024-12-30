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

    public DbSet<Region> Rooms => Set<Region>();

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

        var MobilesConverter = new ValueConverter<ICollection<Mobile>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ICollection<Mobile>>(v, (JsonSerializerOptions?)null)!
        );

        var ObjectsConverter = new ValueConverter<ICollection<Entities.Object>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ICollection<Entities.Object>>(v, (JsonSerializerOptions?)null)!
        );

        var RoomsConverter = new ValueConverter<ICollection<Region>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ICollection<Region>>(v, (JsonSerializerOptions?)null)!
        );

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Player>().Property(e => e.Effects).HasConversion(EffectsConverter);
        modelBuilder.Entity<Player>().Property(e => e.Objects).HasConversion(ObjectsConverter);

        modelBuilder.Entity<Region>().Property(e => e.Effects).HasConversion(EffectsConverter);
        modelBuilder.Entity<Region>().Property(e => e.Mobiles).HasConversion(MobilesConverter);
        modelBuilder.Entity<Region>().Property(e => e.Objects).HasConversion(ObjectsConverter);

        foreach (var type in modelBuilder.Model.GetEntityTypes())
        {
            if ((!type.ClrType.IsAbstract) && type.ClrType.IsAssignableTo(typeof(Entity)))
            {
                modelBuilder.Entity(type.ClrType).Property(nameof(Entity.Properties)).HasConversion(DynamicConverter);
            }
        }
    }
}
