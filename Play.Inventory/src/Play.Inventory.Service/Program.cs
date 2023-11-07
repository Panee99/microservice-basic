using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using Polly;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddMongoDb()
                .AddMongoRepository<InventoryItem>("inventoryitems")
                .AddMongoRepository<CatalogItem>("catalogitems")
                .AddMassTransitWithRabbitMQ();

AddCatalogClient(builder);

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

app.Run();

static void AddCatalogClient(WebApplicationBuilder builder)
{
#pragma warning disable
    builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https://localhost:5001"))
                    .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                    })
                    .AddTransientHttpErrorPolicy(options => options.Or<TimeoutRejectedException>().WaitAndRetryAsync(
                        5,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timeSpan, retryAttempt) =>
                        {
                            var serviceProvider = builder.Services.BuildServiceProvider();
                            serviceProvider.GetService<ILogger<CatalogClient>>()?.LogWarning($"Delaying for {timeSpan.TotalSeconds} seconds, then making retry {retryAttempt}");
                        }
                    ))
                    .AddTransientHttpErrorPolicy(options => options.Or<TimeoutRejectedException>().CircuitBreakerAsync(
                        3,
                        TimeSpan.FromSeconds(15),
                        onBreak: (outcome, timeSpan) =>
                        {
                            var serviceProvider = builder.Services.BuildServiceProvider();
                            serviceProvider.GetService<ILogger<CatalogClient>>()?
                                .LogWarning($"Opening the circuit for {timeSpan.TotalSeconds} seconds...");
                        },
                        onReset: () =>
                        {
                            var serviceProvider = builder.Services.BuildServiceProvider();
                            serviceProvider.GetService<ILogger<CatalogClient>>()?
                                .LogWarning($"Closing the circuit...");
                        }
                    ))
                    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
#pragma warning enable
}