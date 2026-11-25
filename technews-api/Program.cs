using Mapster;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using technews_api.Authentication;
using technews_api.Data;
using technews_api.Mapping;

var builder = WebApplication.CreateBuilder(args);


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

// Autenticacion por API key para la ingesta de datos mediante n8n
string ingestApiKey = builder.Configuration["Ingest:ApiKey"] is { Length: >= 32 } key
    ? key
    : throw new InvalidOperationException(
        "Falta Ingest:ApiKey (mín. 32 caracteres). Defínela con la variable de entorno Ingest__ApiKey.");

// Sin esquema por defecto: el handler solo se ejecuta en los endpoints que lo piden
builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName,
        o => o.ApiKey = ingestApiKey);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IngestOnly", policy => policy
        .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
        .RequireAuthenticatedUser());
});

builder.Services.AddCors(options =>
{
    // Politica de CORS: solo frontend y solo lectura.
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("https://technewsapp.vercel.app", "http://localhost:4200")
        .WithMethods("GET")
        .AllowAnyHeader());
});

var app = builder.Build();

app.UseHttpsRedirection();

// Habilita la compresion del response
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.UseOutputCache();

app.MapControllers();


app.Run();
