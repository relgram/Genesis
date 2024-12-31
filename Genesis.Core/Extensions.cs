using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Genesis.Core;

public static class Extensions
{
    public static IServiceCollection AddGenesis(this IServiceCollection @this)
    {
        @this.AddSingleton<Content.Manager>();
        @this.AddSingleton<Network.Manager>();
        @this.AddSingleton<Runtime.Manager>();
        @this.AddSingleton<GameEngine>();
        return @this;
    }

    public static T? Find<T>(this IEnumerable<T> @this, Predicate<T> match, ref int index)
    {
        foreach (var item in @this)
        {
            if (match(item) == true)
            {
                if (index <= 0)
                {
                    return item;
                }

                index -= 1;
            }
        }

        return default;
    }

    public static void FireAndForget(this Task @this, Action<Exception>? callback = null)
    {
        if ((@this.IsCompleted == false) || (@this.IsFaulted == true))
        {
            _ = ForgetAwaited(@this, callback);
        }

        async static Task ForgetAwaited(Task task, Action<Exception>? callback = null)
        {
            try { await task.ConfigureAwait(false); } catch (Exception ex) { callback?.Invoke(ex); }
        }
    }

    public static void ForEach<T>(this IEnumerable<T> @this, Action<T> action)
    {
        var exceptions = new List<Exception>();

        foreach (var item in @this)
        {
            try
            {
                action(item);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }
    }

    public static byte[] ToBytes(this string @this)
    {
        return string.IsNullOrWhiteSpace(@this) ? [] : Encoding.UTF8.GetBytes(@this);
    }
}
