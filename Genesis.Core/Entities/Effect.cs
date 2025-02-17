using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Effect : Entity
{
    [JsonConstructor]
    public Effect(string name) : base(name)
    {
    }

    public void Load(Driver driver, Region parent, bool save = false)
    {
        Load(driver);

        parent.Register(this);

        if (save == true)
        {
            driver.Content.Save(parent);
        }
    }
}
