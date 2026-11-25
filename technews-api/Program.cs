using Mapster;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
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

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

builder.Services.AddOutputCache(options =>
{
    // Cache para respuestas 200. Una entrada por combinación de query params.
    // Se invalida desde IngestNews (tag "news") cuando entran noticias nuevas.
    options.AddPolicy("news", policy => policy
        .Expire(TimeSpan.FromMinutes(10))
        .SetVaryByQuery("*")
        .Tag("news"));
});
builder.Services.AddCors(options =>
{
    // Politica de CORS
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

// Habilita la compresion del response
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseCors();

app.UseOutputCache();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();
