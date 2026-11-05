using MassTransit;
using Microsoft.Extensions.Hosting;
using NHS111.AdastraConsumer.Consumers;
using NHS111.Infrastructure.Http;
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

        // Register AdastraHttpClient as typed client
        var adastraBaseUrl = configuration["ExternalApis:AdastraBaseUrl"] ?? "http://localhost:7002";
        if (!adastraBaseUrl.EndsWith('/'))
        {
            adastraBaseUrl += "/";
        }

        services.AddHttpClient<AdastraHttpClient>(client =>
        {
            client.BaseAddress = new Uri(adastraBaseUrl);
        })
        .AddPolicyHandler(GetHttpRetryPolicy());

        // AddMassTransit with RabbitMQ:
        //   Consumers: AdastraDispositionConsumer, AdastraStatusConsumer
        //   Queue: "disposition.adastra"
        //   Retry: 3 times, interval 2s/4s/8s
        services.AddMassTransit(x =>
        {
            x.AddConsumer<AdastraDispositionConsumer>();
            x.AddConsumer<AdastraStatusConsumer>();

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

                cfg.ReceiveEndpoint("disposition.adastra", e =>
                {
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(4),
                        TimeSpan.FromSeconds(8)));

                    e.ConfigureConsumer<AdastraDispositionConsumer>(context);
                    e.ConfigureConsumer<AdastraStatusConsumer>(context);
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
