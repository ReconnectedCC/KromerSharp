using System.Text.Json.Serialization;
using Kromer;
using Kromer.Data;
using Kromer.Models.Api.Krist;
using Kromer.Models.Dto;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Repositories;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<KromerContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"),
        o => o.MapEnum<TransactionType>("transaction_type", "public")));

builder.Services.AddScoped<WalletRepository>();
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<NameRepository>();
builder.Services.AddScoped<MiscRepository>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

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