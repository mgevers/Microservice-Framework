using MediatR;
using System.Threading.Channels;

namespace Common.Testing.Mediator;

public sealed class FakeMediator : IMediator, IDisposable
{
    private readonly AsyncLocal<object?> _sendResponse = new();

    private readonly List<object> _sendChannel = [];
    private readonly List<object> _publishChannel = [];

    private FakeMediator(object? sendResponse)
    {
        _sendResponse.Value = sendResponse;
    }

    public static FakeMediator WithSendResponse(object? sendResponse = null)
    {
        return new FakeMediator(sendResponse);
    }

    public IReadOnlyList<object> SentMessages => _sendChannel.ToList().AsReadOnly();

    public IReadOnlyList<object> PublishedMessages => _publishChannel.ToList().AsReadOnly();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        _publishChannel.Add(notification);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        _publishChannel.Add(notification);
        return Task.CompletedTask;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        _sendChannel.Add(request);

        if (_sendResponse.Value is null)
        {
            throw new InvalidOperationException("No response was set for this request.");
        }

        return Task.FromResult((TResponse)_sendResponse.Value);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        _sendChannel.Add(request);
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        _sendChannel.Add(request);
        return Task.FromResult(_sendResponse.Value);
    }

    public void Dispose()
    {
        _sendResponse.Value = null;

        _sendChannel.Clear();
        _publishChannel.Clear();
    }
}
