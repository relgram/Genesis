using Genesis.Core.Content;
using Genesis.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.Loader;
using System.Text;

namespace Genesis.Core.Runtime;

public sealed class Manager : IDisposable
{
    private interface IProcessRequest
    {
    }

    public record ActionRequest(Action Action, Player Sender, string Message) : IProcessRequest;

    public record ProcedureRequest(Procedure Procedure, Entity Sender, string Method, object[] Args) : IProcessRequest;

    private readonly ConcurrentDictionary<string, Action> _actions = [];
    private Task? _backgroundWorker;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ILogger<Manager> _logger;
    private readonly ConcurrentDictionary<string, Procedure> _procedures = [];
    private readonly ConcurrentQueue<IProcessRequest> _requestQueue = [];

    public Manager(ILogger<Manager> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        LibraryPath = configuration["Genesis:LibraryPath"] ?? throw new ArgumentException("LibraryPath not defined");
    }

    public string LibraryPath { get; }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }

    public void DoAction(Player sender, string message)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentException.ThrowIfNullOrEmpty(message);

        try
        {
            var name = message.Split(' ')[0].ToUpper();

            if (_actions.TryGetValue(name, out Action? action) == true)
            {
                _requestQueue.Enqueue(new ActionRequest(action, sender, message));
                return;
            }

            foreach (var item in _actions)
            {
                if (item.Key.Length >= name.Length)
                {
                    if (item.Key.StartsWith(name, true, null))
                    {
                        _requestQueue.Enqueue(new ActionRequest(item.Value, sender, message));
                        return;
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

    public bool DoProcedure(Entity sender, string name, string method, params object[] args)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(method);

        try
        {
            name = name.Split(' ')[0].ToUpper();

            if (_procedures.TryGetValue(name, out Procedure? procedure) == true)
            {
                _requestQueue.Enqueue(new ProcedureRequest(procedure, sender, method, args));
                return true;
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

            File.Copy(LibraryPath, fileName, overwrite: true);

            var context = new AssemblyLoadContext("Genesis", isCollectible: true);

            sender?.Client?.SendBytes(Encoding.UTF8.GetBytes("Loading Library...\n>\n"));

            var assembly = context.LoadFromAssemblyPath(fileName);

            foreach (var type in assembly.GetTypes().Where(x => x.IsAbstract == false))
            {
                if (type.IsAssignableTo(typeof(Action)) == true)
                {
                    if (Activator.CreateInstance(type) is Action action)
                    {
                        if (actions.TryAdd(action.Name.ToUpper(), action) == false)
                        {
                            _logger.LogWarning("Duplicate action found: {Action}", action.Name);
                        }

                        if (action.Alias is not null)
                        {
                            if (actions.TryAdd(action.Alias.ToUpper(), action) == false)
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
                        if (procedures.TryAdd(procedure.Name.ToUpper(), procedure) == false)
                        {
                            _logger.LogWarning("Duplicate procedure found: {Procedure}", procedure.Name);
                        }
                    }
                }
            }

            _actions.Clear();

            _procedures.Clear();

            foreach (var action in actions)
            {
                if (_actions.TryAdd(action.Key, action.Value) == false)
                {
                    sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"Failed to add {action.Key} Action\n"));
                }
            }

            foreach (var procedure in procedures)
            {
                if (_procedures.TryAdd(procedure.Key, procedure.Value) == false)
                {
                    sender?.Client?.SendBytes(Encoding.UTF8.GetBytes($"Failed to add {procedure.Key} Procedure\n"));
                }
            }

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

        LoadLibrary(sender: null);

        _backgroundWorker = Task.Factory.StartNew(() => ProcessAsync(driver), cancellationToken,
            TaskCreationOptions.LongRunning, TaskScheduler.Default)
            .Unwrap();

        _logger.LogInformation("Runtime Manager Started");
    }

    internal void Stop(Driver driver, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(driver);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Stopping Runtime Manager...");

        _cancellationTokenSource.Cancel();

        Task.WaitAll
        (
            _backgroundWorker ?? Task.CompletedTask
        );

        _cancellationTokenSource.Dispose();

        _logger.LogInformation("Runtime Manager Stopped");
    }

    private Task ProcessAsync(Driver driver)
    {
        byte[] ActionFailedBytes = Encoding.UTF8.GetBytes("I could not find what you are referring to.\n>\n");

        while (_cancellationTokenSource.IsCancellationRequested == false)
        {
            try
            {
                if (_requestQueue.TryDequeue(out IProcessRequest? request) == true)
                {
                    if (request is ActionRequest actionRequest)
                    {
                        if (actionRequest.Sender.IsRegistered == true)
                        {
                            if (actionRequest.Action.TryExecute(driver, actionRequest.Sender, actionRequest.Message) == false)
                            {
                                actionRequest.Sender.Client?.SendBytes(ActionFailedBytes);
                            }
                        }
                    }

                    if (request is ProcedureRequest procedureRequest)
                    {
                        if (procedureRequest.Sender.IsRegistered == true)
                        {
                            procedureRequest.Procedure.TryExecute(driver, procedureRequest.Sender, procedureRequest.Method, procedureRequest.Args);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ProcessActionsAsync failed unexpectedly");
            }
        }

        return Task.CompletedTask;
    }
}
