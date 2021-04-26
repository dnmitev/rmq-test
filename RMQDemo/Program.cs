namespace EventContracts
{
    public interface ValueEntered
    {
        string Value { get; }
    }
}


namespace ConsoleEventListener
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using EventContracts;

    using MassTransit;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;

    public class Program
    {
        public static async Task Main()
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(Environment.GetEnvironmentVariable("RMQ_URL"), c =>
                {
                    c.Username("guest");
                    c.Password("guest");
                });

                cfg.ReceiveEndpoint("event-listener", e =>
                {
                    e.Consumer<EventConsumer>();
                });
            });

            var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await busControl.StartAsync(source.Token);
            try
            {
                while (true)
                {
                    var datetime = DateTime.UtcNow;
                    if (datetime.Second % 5 == 0)
                    {
                        await busControl.Publish<ValueEntered>(new
                        {
                            Value = $"{DateTime.UtcNow}-{Guid.NewGuid():N}"
                        });
                    }                   
                }
            }
            finally
            {
                await busControl.StopAsync();
            }
        }

        class EventConsumer :
                IConsumer<ValueEntered>
        {
            public async Task Consume(ConsumeContext<ValueEntered> context)
            {
                Console.WriteLine("Value: {0}", context.Message.Value);
            }
        }
    }
}