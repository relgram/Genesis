using System.Text.Json.Serialization;
using Genesis.Core.Entities;

namespace Genesis.Core.Content.Serialization;

[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(Dynamic))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Actor[]))]
[JsonSerializable(typeof(Area[]))]
[JsonSerializable(typeof(Effect[]))]
[JsonSerializable(typeof(Item[]))]
[JsonSerializable(typeof(Locker[]))]
[JsonSerializable(typeof(Player[]))]
[JsonSerializable(typeof(Portal[]))]
[JsonSerializable(typeof(Room[]))]
[JsonSerializable(typeof(Zone[]))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class EntityContext : JsonSerializerContext
{
}
