namespace Genesis.Core.Tests.Entities;

public class EntityViaPlayerTests
{
    #region Construction

    [Fact]
    public void Constructor_WithName_ShouldSetName()
    {
        var player = new Genesis.Core.Entities.Player("test_name");

        Assert.Equal("test_name", player.Name);
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        var player1 = new Genesis.Core.Entities.Player("a");
        var player2 = new Genesis.Core.Entities.Player("b");

        Assert.NotEqual(player1.Id, player2.Id);
    }

    [Fact]
    public void Constructor_ShouldSetValidGuid()
    {
        var player = new Genesis.Core.Entities.Player("test");

        Assert.NotEqual(Guid.Empty, player.Id);
    }

    [Fact]
    public void Constructor_WithNull_ShouldThrowArgumentNullException()
    {
        var act = () => new Genesis.Core.Entities.Player(null!);

        Assert.Throws<ArgumentNullException>(act);
    }

    #endregion

    #region Register

    [Fact]
    public void Register_WithEffect_ShouldAddToEntities()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        var effect = new Genesis.Core.Entities.Effect("buff");

        player.Register(effect);

        Assert.Single(player.Effects);
    }

    [Fact]
    public void Register_WithObject_ShouldAddToEntities()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        var obj = new Genesis.Core.Entities.Object("sword");

        player.Register(obj);

        Assert.Single(player.Objects);
    }

    [Fact]
    public void Register_WithNullEffect_ShouldThrowArgumentNullException()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        Genesis.Core.Entities.Effect? nullEffect = null;

        var act = () => player.Register(nullEffect!);

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Register_WithNullObject_ShouldThrowArgumentNullException()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        Genesis.Core.Entities.Object? nullObject = null;

        var act = () => player.Register(nullObject!);

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Register_MultipleEntities_ShouldTrackAll()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        player.Register(new Genesis.Core.Entities.Effect("effect1"));
        player.Register(new Genesis.Core.Entities.Effect("effect2"));
        player.Register(new Genesis.Core.Entities.Object("item1"));

        Assert.Equal(2, player.Effects.Count);
        Assert.Single(player.Objects);
    }

    [Fact]
    public void Register_ShouldSetParentReference()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        var effect = new Genesis.Core.Entities.Effect("buff");

        player.Register(effect);

        Assert.Same(player, effect.Parent);
    }

    #endregion

    #region Unregister

    [Fact]
    public void Unregister_WithRegisteredEffect_ShouldRemove()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        var effect = new Genesis.Core.Entities.Effect("buff");

        player.Register(effect);
        player.Unregister(effect);

        Assert.Empty(player.Effects);
    }

    [Fact]
    public void Unregister_WithRegisteredObject_ShouldRemove()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        var obj = new Genesis.Core.Entities.Object("sword");

        player.Register(obj);
        player.Unregister(obj);

        Assert.Empty(player.Objects);
    }

    [Fact]
    public void Unregister_WithNullEffect_ShouldThrowArgumentNullException()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        Genesis.Core.Entities.Effect? nullEffect = null;

        var act = () => player.Unregister(nullEffect!);

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Unregister_WithNullObject_ShouldThrowArgumentNullException()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        Genesis.Core.Entities.Object? nullObject = null;

        var act = () => player.Unregister(nullObject!);

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Unregister_WithUnregisteredEntity_ShouldThrowArgumentException()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        var orphan = new Genesis.Core.Entities.Effect("orphan");

        var act = () => player.Unregister(orphan);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Unregister_ShouldClearParentReference()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        var effect = new Genesis.Core.Entities.Effect("buff");

        player.Register(effect);
        player.Unregister(effect);

        Assert.Null(effect.Parent);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnName()
    {
        var player = new Genesis.Core.Entities.Player("hello");

        Assert.Equal("hello", player.ToString());
    }

    [Fact]
    public void ToString_EmptyName_ShouldReturnEmpty()
    {
        var player = new Genesis.Core.Entities.Player("");

        Assert.Equal("", player.ToString());
    }

    #endregion

    #region Parent

    [Fact]
    public void Parent_Property_ShouldBeNullInitially()
    {
        var effect = new Genesis.Core.Entities.Effect("test");

        Assert.Null(effect.Parent);
    }

    #endregion

    #region Typed Collections

    [Fact]
    public void Effects_Property_ShouldReturnOnlyEffects()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        player.Register(new Genesis.Core.Entities.Effect("effect1"));
        player.Register(new Genesis.Core.Entities.Effect("effect2"));
        player.Register(new Genesis.Core.Entities.Object("sword"));

        Assert.Equal(2, player.Effects.Count);
    }

    [Fact]
    public void Objects_Property_ShouldReturnOnlyObjects()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        player.Register(new Genesis.Core.Entities.Effect("effect1"));
        player.Register(new Genesis.Core.Entities.Object("sword"));
        player.Register(new Genesis.Core.Entities.Object("shield"));

        Assert.Equal(2, player.Objects.Count);
    }

    [Fact]
    public void Effects_Property_ShouldBeReadOnly()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        var effects = (ICollection<Genesis.Core.Entities.Effect>)player.Effects;

        Action act = () => { effects.Add(new Genesis.Core.Entities.Effect("intruder")); };

        Assert.Throws<NotSupportedException>(act);
    }

    [Fact]
    public void Objects_Property_ShouldBeReadOnly()
    {
        var player = new Genesis.Core.Entities.Player("hero");
        var objects = (ICollection<Genesis.Core.Entities.Object>)player.Objects;

        Action act = () => { objects.Add(new Genesis.Core.Entities.Object("intruder")); };

        Assert.Throws<NotSupportedException>(act);
    }

    #endregion
}
