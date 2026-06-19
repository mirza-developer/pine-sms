using System.Threading.Channels;
using PineAI.Core.Features.Order;

namespace PineAI.Api.Queue;

public sealed class OrderNotifyQueue
{
    private readonly Channel<NotifyOrderCommand> _channel =
        Channel.CreateUnbounded<NotifyOrderCommand>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<NotifyOrderCommand> Writer => _channel.Writer;
    public ChannelReader<NotifyOrderCommand> Reader => _channel.Reader;
}
