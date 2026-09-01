using System.Text;
using System.Text.Json.Serialization;
using Bookings.Application.Bookings.Commands.CompletingBooking;
using Bookings.Application.Bookings.Commands.CreateBooking;
using Bookings.Application.Bookings.Commands.RejectBooking;
using Bookings.Application.Common.Config;
using Bookings.Infrastructure.Data.Extensions;
using Contracts.Common;
using Contracts.Kafka;
using Contracts.Messages;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateBookingHandler).Assembly);
});


builder.Services.AddKafkaConsumer<ConfirmationBookingMessage, CompletingBookingCommand>(builder.Configuration, TopicNames.BookingConfirmation, "booking1",
    message => new CompletingBookingCommand
    {
        BookingId = new Guid(message.BookingId)
    }
);

builder.Services.AddKafkaConsumer<RejectBookingMessage, RejectBookingCommand>(builder.Configuration, TopicNames.BookingReject, "booking2",
    message => new RejectBookingCommand
    {
        BookingId = new Guid(message.BookingId),
        Error = message.Error
    }
);

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
builder.Services.Configure<BookingSettings>(
    builder.Configuration.GetSection("Booking")
);

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