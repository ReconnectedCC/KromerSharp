using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Xml.XPath;
using Kromer;
using Kromer.Data;
using Kromer.Models.Api.Krist;
using Kromer.Models.Api.V1;
using Kromer.Models.Entities;
using Kromer.Models.Exceptions;
using Kromer.Models.WebSocket.Events;
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
        o =>
        {
            o.MapEnum<TransactionType>("transaction_type", "public");
            o.MapEnum<SubscriptionStatus>("subscription_status", "public");
        }));

builder.Services.AddScoped<WalletRepository>();
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<NameRepository>();
builder.Services.AddScoped<MiscRepository>();
builder.Services.AddScoped<PlayerRepository>();
builder.Services.AddScoped<SearchRepository>();
builder.Services.AddScoped<SubscriptionRepository>();

builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<DiscordService>();

builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton(Channel.CreateUnbounded<IKristEvent>());

builder.Services.AddHostedService<EventDispatcher>();
builder.Services.AddHostedService<BackgroundSessionJob>();
builder.Services.AddHostedService<SubscriptionBillingService>();

// Support for reverse proxies, like NGINX
builder.Services.Configure<ForwardedHeadersOptions>(options => { options.ForwardedHeaders = ForwardedHeaders.All; });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(o => o
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
    );
});

builder.Services.AddControllers(options =>
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        options.InputFormatters.Add(new IgnoreContentTypeJsonInputFormatter(jsonOptions));
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
        //options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(o =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (!File.Exists(xmlPath)) return;

    var xmlDoc = new XPathDocument(xmlPath);
    var xmlNav = xmlDoc.CreateNavigator();

    o.AddOperationTransformer((operation, context, ct) =>
    {
        var method = context.Description.ActionDescriptor
            .EndpointMetadata
            .OfType<MethodInfo>()
            .FirstOrDefault();

        if (method is null) return Task.CompletedTask;

        var memberName = $"M:{method.DeclaringType!.FullName}.{method.Name}({string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName))})";
        var summary = xmlNav.SelectSingleNode($"//member[@name='{memberName}']/summary");
        if (summary != null)
            operation.Summary = System.Text.RegularExpressions.Regex.Replace(summary.InnerXml.Trim(), @"\s+", " ");

        var remarks = xmlNav.SelectSingleNode($"//member[@name='{memberName}']/remarks");
        if (remarks != null)
            operation.Description = System.Text.RegularExpressions.Regex.Replace(remarks.InnerXml.Trim(), @"\s+", " ");

        if (summary != null)
            operation.Summary = summary.InnerXml.Trim();

        return Task.CompletedTask;
    });
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
app.MapOpenApi(pattern: "/openapi/v1.json");

app.UseHttpsRedirection();

app.UseAuthorization();

app.Use(async (context, next) =>
{
    var extra = new List<string>();
    if (context.Request.Headers.TryGetValue("X-Cc-Id", out var computercraftId))
    {
        extra.Add($"(ComputerCraft ID: {computercraftId})");
    }

    app.Logger.LogInformation("{IpAddress} {Method} {Path}{QueryString} '{UserAgent}' {Extra}",
        context.Connection.RemoteIpAddress, context.Request.Method, context.Request.Path, context.Request.QueryString,
        context.Request.Headers.UserAgent, string.Join(" ", extra));

    await next();
});

app.UseWebSockets();

app.UseCors();

app.MapControllers();

app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "Kromer API V1");
    o.RoutePrefix = string.Empty;
});

app.UseExceptionHandler(builder =>
{
    builder.Run(async context =>
    {
        try
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
        }
        catch (Exception ex)
        {
            // When in doubt...
            app.Logger.LogError(ex, "Error processing request");
        }
    });
});

app.Run();