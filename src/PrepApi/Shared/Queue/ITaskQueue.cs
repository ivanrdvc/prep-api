using System.Threading.Channels;

namespace PrepApi.Shared.Queue;

public interface ITaskQueue
{
    ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}

public class InMemoryTaskQueue : ITaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;
    private readonly ILogger<InMemoryTaskQueue> _logger;

    public InMemoryTaskQueue(ILogger<InMemoryTaskQueue> logger, int capacity = 1000)
    {
        _logger = logger;

        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public async ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        try
        {
            await _queue.Writer.WriteAsync(workItem);
            _logger.LogInformation("Queued background work item");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue background work item");
            throw;
        }
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}

public class TaskProcessor : BackgroundService
{
    private readonly ITaskQueue _taskQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TaskProcessor> _logger;

    public TaskProcessor(
        ITaskQueue taskQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<TaskProcessor> logger)
    {
        _taskQueue = taskQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation("Processing background work item");

                await workItem(stoppingToken);

                _logger.LogInformation("Completed background work item");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing background work item");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        _logger.LogInformation("Task Processor stopped");
    }
}