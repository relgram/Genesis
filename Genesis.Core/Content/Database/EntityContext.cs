using System.Text.Json;
using Genesis.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Genesis.Core.Content.Database;

internal sealed class EntityContext : DbContext
{
    public DbSet<Actor> Actors => Set<Actor>();

    public DbSet<Area> Areas => Set<Area>();

    public DbSet<Item> Items => Set<Item>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Zone> Zones => Set<Zone>();

    public EntityContext(DbContextOptions options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var DynamicConverter = new ValueConverter<Dictionary<string, Dynamic>, string>
        (
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, Dynamic>>(v, (JsonSerializerOptions?)null)!
        );

        base.OnModelCreating(modelBuilder);

        foreach (var type in modelBuilder.Model.GetEntityTypes())
        {
            if ((!type.ClrType.IsAbstract) && type.ClrType.IsAssignableTo(typeof(Entity)))
            {
                modelBuilder.Entity(type.ClrType).Property(nameof(Entity.Properties)).HasConversion(DynamicConverter);
            }
        }
    }
}
