using MassTransit;
using Microsoft.Extensions.Hosting;
using NHS111.Infrastructure.Http;
using NHS111.UmmanuConsumer.Consumers;
using Polly;
using Polly.Extensions.Http;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, services, configuration) =>
    {
        configuration
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console();
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        // AddHttpClient "UmmanuClient" with base URL from config + Polly retry
        var ummanuBaseUrl = configuration["ExternalApis:UmmanuBaseUrl"] ?? "http://localhost:7001";
        if (!ummanuBaseUrl.EndsWith('/'))
        {
            ummanuBaseUrl += "/";
        }

        services.AddHttpClient("UmmanuClient", client =>
        {
            client.BaseAddress = new Uri(ummanuBaseUrl);
        })
        .AddPolicyHandler(GetHttpRetryPolicy());

        // Register UmmanuHttpClient as typed client
        services.AddHttpClient<UmmanuHttpClient>(client =>
        {
            client.BaseAddress = new Uri(ummanuBaseUrl);
        })
        .AddPolicyHandler(GetHttpRetryPolicy());

        // AddMassTransit with RabbitMQ:
        //   Consumer: UmmanuDispositionConsumer
        //   Queue: "disposition.ummanu"
        //   Retry: 3 times, interval 2s/4s/8s
        services.AddMassTransit(x =>
        {
            x.AddConsumer<UmmanuDispositionConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
                var rabbitUser = configuration["RabbitMQ:Username"] ?? "guest";
                var rabbitPass = configuration["RabbitMQ:Password"] ?? "guest";
                var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";

                cfg.Host(rabbitHost, virtualHost, h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                cfg.ReceiveEndpoint("disposition.ummanu", e =>
                {
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(4),
                        TimeSpan.FromSeconds(8)));

                    e.ConfigureConsumer<UmmanuDispositionConsumer>(context);
                });
            });
        });
    })
    .Build();

await host.RunAsync();

static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync([
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(8)
        ]);
}
