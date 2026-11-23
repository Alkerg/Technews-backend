using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using technews_api.Data;
using technews_api.Mapping;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string mongoConnectionString = builder.Configuration.GetConnectionString("MongoConnection") is { Length: > 0 } cs
    ? cs
    : throw new InvalidOperationException(
        "Falta ConnectionStrings:MongoConnection. Defínela con la variable de entorno ConnectionStrings__MongoConnection.");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
TypeAdapterConfig.GlobalSettings.Scan(typeof(NewsMappingConfig).Assembly);
builder.Services.AddMapster();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<NewsDbContext>(options => options.UseMongoDB(
    mongoConnectionString, "newstech"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();

}


app.UseAuthorization();

app.MapControllers();

app.Run();
