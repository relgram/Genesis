using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Genesis.Core.Tests.Extensions;

public class AddGenesisTests
{
    [Fact]
    public void AddGenesis_ShouldRegisterContentManager()
    {
        var services = new ServiceCollection();
        services.AddGenesis();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<Genesis.Core.Content.Manager>());
    }

    [Fact]
    public void AddGenesis_ShouldRegisterNetworkManager()
    {
        var services = new ServiceCollection();
        services.AddGenesis();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<Genesis.Core.Network.Manager>());
    }

    [Fact]
    public void AddGenesis_ShouldRegisterRuntimeManager()
    {
        var services = new ServiceCollection();
        services.AddGenesis();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<Genesis.Core.Runtime.Manager>());
    }

    [Fact]
    public void AddGenesis_ShouldRegisterDriver()
    {
        var services = new ServiceCollection();
        services.AddGenesis();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<Genesis.Core.Driver>());
    }
}

public class FindTests
{
    [Fact]
    public void Find_WithMatchingItemAtZero_ShouldReturnFirstMatch()
    {
        var list = new List<string> { "apple", "banana", "cherry" };
        int index = 0;

        var result = list.Find(x => x.Length > 4, ref index);

        Assert.Equal("apple", result);
    }

    [Fact]
    public void Find_WithIndexGreaterThanZero_ShouldSkipMatches()
    {
        var list = new List<string> { "apple", "banana", "cherry", "date" };
        int index = 1;

        var result = list.Find(x => x.Length > 4, ref index);

        Assert.Equal("banana", result);
    }

    [Fact]
    public void Find_WithNoMatches_ShouldReturnDefault()
    {
        var list = new List<int> { 1, 2, 3 };
        int index = 0;

        var result = list.Find(x => x > 100, ref index);

        Assert.Equal(default(int), result);
    }

    [Fact]
    public void Find_WithNullList_ShouldThrowArgumentNullException()
    {
        int index = 0;
        IEnumerable<int>? nullList = null;

        Action act = () => { nullList!.Find(new Predicate<int>(x => x > 0), ref index); };

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Find_WithIndexLargerThanMatches_ShouldReturnDefault()
    {
        var list = new List<string> { "apple", "banana" };
        int index = 5;

        var result = list.Find(x => true, ref index);

        Assert.Null(result);
    }

    [Fact]
    public void Find_WithIndexNegative_ShouldReturnFirstMatch()
    {
        var list = new List<string> { "first", "second" };
        int index = -1;

        var result = list.Find(x => true, ref index);

        Assert.Equal("first", result);
    }
}

public class FireAndForgetTests
{
    [Fact]
    public void FireAndForget_CompletedTask_ShouldNotThrow()
    {
        var task = Task.CompletedTask;

        var act = () => task.FireAndForget();

        // Should not throw
    }

    [Fact]
    public async Task FireAndForget_FaultedTask_WithCallback_ShouldInvokeCallback()
    {
        var exceptionTask = Task.FromException(new InvalidOperationException("test error"));
        Exception? captured = null;

        exceptionTask.FireAndForget(ex => captured = ex);

        // Give the fire-and-forget task time to complete
        await Task.Delay(100);

        Assert.NotNull(captured);
        Assert.IsType<InvalidOperationException>(captured);
    }

    [Fact]
    public void FireAndForget_FaultedTask_WithoutCallback_ShouldNotThrow()
    {
        var exceptionTask = Task.FromException(new InvalidOperationException("test error"));

        var act = () => exceptionTask.FireAndForget();

        // Should not throw synchronously
    }
}

public class ForEachTests
{
    [Fact]
    public void ForEach_WithValidAction_ShouldExecuteForAllItems()
    {
        var list = new List<int> { 1, 2, 3 };
        var results = new List<int>();

        list.ForEach(x => results.Add(x * 2));

        Assert.Equal(new List<int> { 2, 4, 6 }, results);
    }

    [Fact]
    public void ForEach_EmptyCollection_ShouldNotThrow()
    {
        var list = new List<int>();
        bool executed = false;

        list.ForEach(_ => executed = true);

        Assert.False(executed);
    }

    [Fact]
    public void ForEach_WithSingleException_ShouldThrowAggregateException()
    {
        var list = new List<int> { 1, 2, 3 };

        var act = () => list.ForEach(x =>
        {
            if (x == 2) throw new DivideByZeroException();
        });

        var agg = Assert.Throws<AggregateException>(act);
        Assert.Single(agg.InnerExceptions);
        Assert.IsType<DivideByZeroException>(agg.InnerExceptions[0]);
    }

    [Fact]
    public void ForEach_WithMultipleExceptions_ShouldThrowAggregateException()
    {
        var list = new List<int> { 1, 2, 3 };

        var act = () => list.ForEach(x =>
        {
            if (x > 1) throw new InvalidOperationException($"error {x}");
        });

        var agg = Assert.Throws<AggregateException>(act);
        Assert.Equal(2, agg.InnerExceptions.Count);
    }

    [Fact]
    public void ForEach_NullCollection_ShouldThrowArgumentNullException()
    {
        IEnumerable<int>? nullList = null;

        var act = () => nullList!.ForEach(delegate(int _) { });

        Assert.Throws<ArgumentNullException>(act);
    }
}

public class ToBytesTests
{
    [Fact]
    public void ToBytes_WithValidString_ShouldReturnUtf8Bytes()
    {
        var result = "hello".ToBytes();

        Assert.Equal(Encoding.UTF8.GetBytes("hello"), result);
    }

    [Fact]
    public void ToBytes_WithEmptyString_ShouldReturnEmptyArray()
    {
        var result = string.Empty.ToBytes();

        Assert.Empty(result);
    }

    [Fact]
    public void ToBytes_WithNull_ShouldReturnEmptyArray()
    {
        var result = ((string?)null)!.ToBytes();

        Assert.Empty(result);
    }

    [Fact]
    public void ToBytes_WithWhitespace_ShouldReturnEmptyArray()
    {
        var result = "   ".ToBytes();

        Assert.Empty(result);
    }

    [Fact]
    public void ToBytes_WithUnicode_ShouldReturnCorrectBytes()
    {
        var result = "héllo".ToBytes();

        Assert.Equal(Encoding.UTF8.GetBytes("héllo"), result);
    }
}
