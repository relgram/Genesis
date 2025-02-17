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
        _libraryPath = configuration["Genesis:LibraryPath"] ?? throw new ArgumentException("LibraryPath not defined");
    }

    public void DoAction(Driver driver, Player sender, string message)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(sender);
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
                    if (_actions[name].Execute(driver, sender, message) == false)
                    {
                        sender?.Client?.SendBytes(Encoding.UTF8.GetBytes("I could not find what you are referring to.\n>\n"));
                    }

                    return;
                }

                foreach (var item in _actions)
                {
                    if (item.Key.Length >= name.Length)
                    {
                        if (item.Key.StartsWith(name, true, null))
                        {
                            if (item.Value.Execute(driver, sender, message) == false)
                            {
                                sender?.Client?.SendBytes(Encoding.UTF8.GetBytes("I could not find what you are referring to.\r\n>"));
                            }

                            return;
                        }
                    }
                }
            }

            sender?.Client?.SendBytes(Encoding.UTF8.GetBytes("Please rephrase that command.\n>\n"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Action failed unexpectedly");
        }
    }

    public bool DoProcedure(string name, string method, params object[] args)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(method);

        try
        {
            lock (_internalLock)
            {
                if (_procedures.ContainsKey(name) == true)
                {
                    if (_procedures[name].Execute(method, args) == true)
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
            var actions = new Dictionary<string, Action>();
            var procedures = new Dictionary<string, Procedure>();

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
                        if (actions.TryAdd(action.Name, action) == false)
                        {
                            _logger.LogWarning("Duplicate action found: {Action}", action.Name);
                        }

                        if (action.Alias is not null)
                        {
                            if (actions.TryAdd(action.Alias, action) == false)
                            {
                                _logger.LogWarning("Duplicate action found: {Action}", action.Alias);
                            }
                        }
                    }
                }

                if (type.IsAssignableTo(typeof(Procedure)) == true)
                {
                    if (Activator.CreateInstance(type) is Procedure procedure)
                    {
                        if (procedures.TryAdd(procedure.Name, procedure) == false)
                        {
                            _logger.LogWarning("Duplicate procedure found: {Procedure}", procedure.Name);
                        }
                    }
                }
            }

            lock (_internalLock)
            {
                _actions.Clear();

                _procedures.Clear();

                foreach (var action in actions)
                {
                    _actions.Add(action.Key, action.Value);
                }

                foreach (var procedure in procedures)
                {
                    _procedures.Add(procedure.Key, procedure.Value);
                }
            }

            _logger.LogInformation("Loaded {Count} Actions", _actions.Count);
            _logger.LogInformation("Loaded {Count} Procedures", _procedures.Count);

            sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"Loaded {_actions.Count} Actions\n"));
            sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"Loaded {_procedures.Count} Procedures\n>\n"));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "LoadGameplay failed unexpectedly");

            sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"LoadGameplay failed unexpectedly:{ex.Message}"));
        }
    }

    internal void Start(Driver driver, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(driver);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Starting Runtime Manager...");

        LoadLibrary();

        _logger.LogInformation("Runtime Manager Started");
    }

    internal void Stop(Driver driver, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(driver);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Stopping Runtime Manager...");

        lock (_internalLock)
        {
            _actions.Clear();
            _procedures.Clear();
        }

        _logger.LogInformation("Runtime Manager Stopped");
    }
}
