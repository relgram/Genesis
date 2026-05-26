using System.Collections.Concurrent;
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
        @this.AddSingleton<Driver>();
        return @this;
    }

    public static T? Find<T>(this IEnumerable<T> @this, Predicate<T> match, ref int index)
    {
        ArgumentNullException.ThrowIfNull(@this);

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
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                callback?.Invoke(ex);
            }
        }
    }

    public static void ForEach<T>(this IEnumerable<T> @this, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(@this);

        Exception? firstException = null;

        foreach (var item in @this)
        {
            try
            {
                action(item);
            }
            catch (Exception ex)
            {
                if (firstException is null)
                {
                    firstException = ex;
                }
                else
                {
                    var exceptions = new List<Exception> { firstException, ex };
                    firstException = null;

                    foreach (var remaining in @this)
                    {
                        try
                        {
                            action(remaining);
                        }
                        catch (Exception inner)
                        {
                            exceptions.Add(inner);
                        }
                    }

                    throw new AggregateException(exceptions);
                }
            }
        }

        if (firstException is not null)
        {
            throw new AggregateException(firstException);
        }
    }

    public static byte[] ToBytes(this string @this)
    {
        return string.IsNullOrWhiteSpace(@this) ? [] : Encoding.UTF8.GetBytes(@this);
    }
}
