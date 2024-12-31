using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Portal : Entity
{
    [JsonConstructor]
    public Portal(string name) : base(name)
    {
    }

    public void Load(GameEngine engine, Region region)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(region);

        engine.Content.Register(this);

        region.Register(this);
    }
}
