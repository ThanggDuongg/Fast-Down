using System.Reflection;
using FastDown.Core;
using FastDown.Infrastructure.Messaging;
using FastDown.Infrastructure.Persistence;
using FastDown.Infrastructure.Persistence.MongoDB;
using FastDown.Infrastructure.Services;
using FastDown.Worker;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IMediator, Mediator>();

// Register all handlers manually
var handlerTypes = Assembly
    .GetExecutingAssembly()
    .GetTypes()
    .Where(t => !t.IsAbstract && !t.IsInterface)
    .Where(t =>
        t.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<,>))
    )
    .ToList();

foreach (var handlerType in handlerTypes)
{
    var handlerInterface = handlerType
        .GetInterfaces()
        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<,>));

    builder.Services.AddScoped(handlerInterface, handlerType);
}

// Add DbContext
builder.Services.AddDbContext<FDContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(FDContext).Assembly.FullName)
    )
);

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<DownloadTaskReadRepository>();

// Configure RabbitMQ
builder.Services.Configure<FastDown.Infrastructure.Messaging.RabbitMQSettings>(
    builder.Configuration.GetSection("RabbitMQSettings")
);
builder.Services.AddSingleton<IEventPublisher, RabbitMQPublisher>();

// Register HttpClient and DownloadService
builder.Services.AddHttpClient<DownloadService>();
builder.Services.AddScoped<DownloadService>();

// Add RabbitMQ Consumer as a hosted service
builder.Services.AddHostedService<RabbitMQConsumerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync(new CancellationToken());
