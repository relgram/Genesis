using System.ComponentModel.DataAnnotations.Schema;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Portal))]
public sealed class Portal : Entity
{
    public Portal(string name) : base(name)
    {
    }
}
