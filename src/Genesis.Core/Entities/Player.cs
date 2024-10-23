using System.ComponentModel.DataAnnotations.Schema;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Player))]
public sealed class Player : Entity
{
    public Player(string name) : base(name)
    {
    }
}
