using Microsoft.OpenApi.Models;
using Proiect.Infrastructure.Persistence;
using Proiect.Application.Inventory.Handlers;
using Proiect.Application.Shipping.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Proiect PSSC", Version = "v1" });
});

// ═══════════════════════════════════════════════════════════════════════════════
// INVENTORY BOUNDED CONTEXT - Dependency Injection
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IInventoryRepository, InMemoryInventoryRepository>();
builder.Services.AddScoped<InventoryCommandHandlers>();

// ═══════════════════════════════════════════════════════════════════════════════
// SHIPPING & DELIVERY BOUNDED CONTEXT - Dependency Injection
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IShipmentRepository, InMemoryShipmentRepository>();
builder.Services.AddScoped<ShippingCommandHandlers>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();