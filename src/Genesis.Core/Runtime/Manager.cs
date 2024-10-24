using System.Runtime.Loader;
using Genesis.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Genesis.Core.Runtime;

public sealed class Manager
{
    private readonly SortedDictionary<string, Action> _actions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _internalLock = new();
    private readonly string _libraryPath;
    private readonly ILogger<Manager> _logger;
    private readonly SortedDictionary<string, Procedure> _procedures = new(StringComparer.OrdinalIgnoreCase);

    public Manager(ILogger<Manager> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _libraryPath = configuration["Genesis:LibraryPath"] ?? throw new Exception("LibraryPath not defined");
    }

    internal void Start(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();

        LoadLibrary();
    }

    internal void Stop(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public void DoAction(GameEngine engine, string name, string method, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(method);

        try
        {
            lock (_internalLock)
            {
                if (_actions.ContainsKey(name) == true)
                {
                    if (_actions[name].Execute(engine, method, args) == true)
                    {
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Action failed unexpectedly");
        }
    }

    public bool DoProcedure(GameEngine engine, string name, string method, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(method);

        try
        {
            lock (_internalLock)
            {
                if (_procedures.ContainsKey(name) == true)
                {
                    if (_procedures[name].Execute(engine, method, args) == true)
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Procedure failed unexpectedly");
        }

        return false;
    }

    public void LoadLibrary(Player? sender = null)
    {
        try
        {
            lock (_internalLock)
            {
                _actions.Clear();

                _procedures.Clear();

                var fileName = Path.GetTempFileName();

                File.Copy(_libraryPath, fileName, overwrite: true);

                var context = new AssemblyLoadContext("Genesis", isCollectible: true);

                //sender?.Client?.SendBytes(Encoding.UTF8.GetBytes("Loading...\n>\n"));

                var assembly = context.LoadFromAssemblyPath(fileName);

                foreach (var type in assembly.GetTypes().Where(x => x.IsAbstract == false))
                {
                    if (type.IsAssignableTo(typeof(Action)) is true)
                    {
                        if (Activator.CreateInstance(type) is Action action)
                        {
                            if (_actions.TryAdd(action.Name, action) is false)
                            {
                                _logger.LogWarning("Duplicate action found: {action}", action.Name);
                            }
                        }
                    }

                    if (type.IsAssignableTo(typeof(Procedure)) is true)
                    {
                        if (Activator.CreateInstance(type) is Procedure procedure)
                        {
                            if (_procedures.TryAdd(procedure.Name, procedure) is false)
                            {
                                _logger.LogWarning("Duplicate procedure found: {procedure}", procedure.Name);
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Loaded {count} Actions", _actions.Count);
            _logger.LogInformation("Loaded {count} Procedures", _procedures.Count);

            //sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"Loaded {_actions.Count} Actions\n>\n"));
            //sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"Loaded {_procedures.Count} Procedures\n>\n"));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "LoadGameplay Failed Unexpectedly");
            //sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"LoadGameplay Failed Unexpectedly:{ex.Message}"));
        }
    }
}
