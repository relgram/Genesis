using System;
using System.Runtime.Loader;
using System.Text;
using Genesis.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Genesis.Core.Runtime;

public sealed class Manager
{
    private readonly SortedDictionary<string, Action> _actions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _internalLock = new();
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
        _logger.LogInformation("Starting Runtime Manager...");
        LoadLibrary();
        _logger.LogInformation("Runtime Manager Started");
    }

    internal void Stop(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Stopping Runtime Manager...");
        _actions.Clear();
        _procedures.Clear();
        _logger.LogInformation("Runtime Manager Stopped");
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

    public void DoAction(GameEngine engine, Content.Entity entity, string message)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrEmpty(message);

        try
        {
            if (message[0] == '\'')
            {
                message = $"say {message[1..]}";
            }

            var name = message.Split(' ')[0];

            lock (_internalLock)
            {
                if (_actions.ContainsKey(name) == true)
                {
                    if (_actions[name].Execute(engine, entity, message) == false)
                    {
                        entity?.Client?.SendBytes(Encoding.UTF8.GetBytes("I could not find what you were referring to.\n>\n"));
                    }

                    return;
                }

                foreach (var item in _actions)
                {
                    if (item.Key.Length >= name.Length)
                    {
                        if (item.Key.StartsWith(name, true, null))
                        {
                            if (item.Value.Execute(engine, entity, message) == false)
                            {
                                entity?.Client?.SendBytes(Encoding.UTF8.GetBytes("I could not find what you were referring to.\r\n>"));
                            }

                            return;
                        }
                    }
                }
            }

            entity?.Client?.SendBytes(Encoding.UTF8.GetBytes("Please rephrase that command.\n>\n"));
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

                sender?.Client?.SendBytes(Encoding.UTF8.GetBytes("Loading Library...\n>\n"));

                var assembly = context.LoadFromAssemblyPath(fileName);

                foreach (var type in assembly.GetTypes().Where(x => x.IsAbstract == false))
                {
                    if (type.IsAssignableTo(typeof(Action)) == true)
                    {
                        if (Activator.CreateInstance(type) is Action action)
                        {
                            if (_actions.TryAdd(action.Name, action) == false)
                            {
                                _logger.LogWarning("Duplicate action found: {action}", action.Name);
                            }

                            if (action.Alias is not null)
                            {
                                if (_actions.TryAdd(action.Alias, action) == false)
                                {
                                    _logger.LogWarning("Duplicate action found: {action}", action.Alias);
                                }
                            }
                        }
                    }

                    if (type.IsAssignableTo(typeof(Procedure)) == true)
                    {
                        if (Activator.CreateInstance(type) is Procedure procedure)
                        {
                            if (_procedures.TryAdd(procedure.Name, procedure) == false)
                            {
                                _logger.LogWarning("Duplicate procedure found: {procedure}", procedure.Name);
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Loaded {count} Actions", _actions.Count);
            _logger.LogInformation("Loaded {count} Procedures", _procedures.Count);

            sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"Loaded {_actions.Count} Actions\n"));
            sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"Loaded {_procedures.Count} Procedures\n>\n"));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "LoadGameplay failed unexpectedly");

            sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"LoadGameplay failed unexpectedly:{ex.Message}"));
        }
    }
}
