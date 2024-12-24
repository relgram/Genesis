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

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Region> Regions => Set<Region>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var DynamicConverter = new ValueConverter<Dictionary<string, Dynamic>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, Dynamic>>(v, (JsonSerializerOptions?)null)!
        );

        var PlayersConverter = new ValueConverter<ICollection<Player>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ICollection<Player>>(v, (JsonSerializerOptions?)null)!
        );

        var RoomsConverter = new ValueConverter<ICollection<Room>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ICollection<Room>>(v, (JsonSerializerOptions?)null)!
        );

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>().Property(e => e.Players).HasConversion(PlayersConverter);
        modelBuilder.Entity<Region>().Property(e => e.Rooms).HasConversion(RoomsConverter);

        foreach (var type in modelBuilder.Model.GetEntityTypes())
        {
            if ((!type.ClrType.IsAbstract) && type.ClrType.IsAssignableTo(typeof(Entity)))
            {
                modelBuilder.Entity(type.ClrType).Property(nameof(Entity.Properties)).HasConversion(DynamicConverter);
            }
        }
    }
}
