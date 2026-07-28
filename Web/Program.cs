using System.Text.Json.Serialization;
using Application.Abstractions.Persistence.Repositories;
using Application.Events.Commands.CreateEvent;
using Infrastructure;
using Infrastructure.Background;
using Infrastructure.Presentation;
using Infrastructure.Presentation.Repositories;
using Microsoft.EntityFrameworkCore;
using Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateEventHandler).Assembly);
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

await app.Services.ApplyMigrationsAsync();

app.MapControllers();

app.Run();