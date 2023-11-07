using Play.Catalog.Service.Entities;
using Play.Common.MongoDB;
using Play.Common.MassTransit;
using Play.Common.Settings;

var builder = WebApplication.CreateBuilder(args);
var AllowedOriginSetting = "AllowedOrigin";
var configuration = builder.Configuration;
var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>()!;

// Add services to the container.

builder.Services.AddMongoDb()
                .AddMongoRepository<Item>("items");

builder.Services.AddMassTransitWithRabbitMQ();

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors(builder =>
    {
        builder.WithOrigins(configuration[AllowedOriginSetting]!)
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
