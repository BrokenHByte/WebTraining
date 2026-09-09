using System.Text;
using System.Text.Json.Serialization;
using Contracts.Common;
using Contracts.Kafka;
using Contracts.Messages;
using Events.Application.Events.Commands.CreateEvent;
using Events.Application.Events.Commands.ReleaseSeatEvent;
using Events.Application.Events.Commands.ReserveSeat;
using Events.Infrastructure.Cache;
using Events.Infrastructure.Data.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Presentation.Middleware;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateEventHandler).Assembly);
});

builder.Services.AddSingleton<KafkaProducerService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaProducerService>>();
    var server = builder.Configuration["Kafka:BootstrapServers"];
    var cliendId = "event-service";
    return new KafkaProducerService(server, cliendId, logger);
});

builder.Services.AddKafkaConsumer<CreateBookingMessage, ReserveSeatCommand>(builder.Configuration, TopicNames.BookingCreate, "event1", message =>
    new ReserveSeatCommand()
    {
        BookingId = message.BookingId,
        UserId = message.UserId,
        EventId = message.EventId
    });

builder.Services.AddKafkaConsumer<CancelledBookingMessage, ReleaseSeatEventCommand>(builder.Configuration, TopicNames.BookingCancelled, "event2", message =>
    new ReleaseSeatEventCommand()
    {
        EventId = new Guid(message.EventId)
    });

var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("JWT key is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            )
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Description = "JWT Authorization",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, doc)] = []
    });

});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});


var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

await app.Services.ApplyMigrationsAsync();

app.MapControllers();

app.Run();