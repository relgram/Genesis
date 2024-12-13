using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Effect : Entity
{
    [JsonConstructor]
    public Effect(string name) : base(name)
    {
    }
}