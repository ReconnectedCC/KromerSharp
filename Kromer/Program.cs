using System.Text.Json.Serialization;
using System.Threading.Channels;
using Kromer;
using Kromer.Data;
using Kromer.Models.Api.Krist;
using Kromer.Models.Api.V1;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Repositories;
using Kromer.Services;
using Kromer.SessionManager;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDbContext<KromerContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"),
        o => o.MapEnum<TransactionType>("transaction_type", "public")));

builder.Services.AddScoped<WalletRepository>();
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<NameRepository>();
builder.Services.AddScoped<MiscRepository>();
builder.Services.AddScoped<PlayerRepository>();

builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<SessionService>();

builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton(Channel.CreateUnbounded<KristEvent>());

builder.Services.AddHostedService<EventDispatcher>();
builder.Services.AddHostedService<BackgroundSessionJob>();

// Support for reverse proxies, like NGINX
builder.Services.Configure<ForwardedHeadersOptions>(options => { options.ForwardedHeaders = ForwardedHeaders.All; });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
        //options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(new SnakeCaseNamingPolicy()));
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseWebSockets();

app.MapControllers();

app.UseExceptionHandler(builder =>
{
    builder.Run(async context =>
    {
        var exception = context.Features
            .Get<IExceptionHandlerFeature>()?
            .Error;

        if (exception is KristException kristException)
        {
            var error = new KristResult
            {
                Ok = false,
                Error = kristException.Error,
                Message = kristException.Message,
            };

            if (exception is KristParameterException parameterException)
            {
                error.Parameter = parameterException.Parameter;
            }

            context.Response.StatusCode = (int)kristException.GetStatusCode();
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(error);

            return;
        }
        else if (exception is KromerException kromerException)
        {
            var result = Result<object>.Throw(new Error
            {
                Code = kromerException.Error,
                Message = kromerException.Message,
                Details = Array.Empty<object>(),
            });
            
            context.Response.StatusCode = (int)kromerException.GetStatusCode();
            context.Response.ContentType = "application/json";
            
            await context.Response.WriteAsJsonAsync(result);

            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new KristResult
        {
            Ok = false,
            Error = "internal_server_error",
        });
    });
});

app.Run();