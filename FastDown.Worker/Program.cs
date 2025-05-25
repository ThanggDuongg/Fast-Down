using FastDown.Worker;
using FastDown.Infrastructure.Persistence.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configure MongoDB
        services.Configure<MongoDbSettings>(
            hostContext.Configuration.GetSection("MongoDbSettings"));
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<DownloadTaskReadRepository>();

        // Configure RabbitMQ Consumer
        services.Configure<RabbitMQSettings>(
            hostContext.Configuration.GetSection("RabbitMQSettings"));
        services.AddHostedService<RabbitMQConsumerService>();
    })
    .Build();

await host.RunAsync();