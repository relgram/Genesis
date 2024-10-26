using System.ComponentModel.DataAnnotations.Schema;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Effect))]
public sealed class Effect : Entity
{
    public Effect(string name) : base(name)
    {
    }
}
