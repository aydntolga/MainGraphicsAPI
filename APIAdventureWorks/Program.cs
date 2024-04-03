using APIAdventureWorks.Controllers;
using APIAdventureWorks.Models;
using APIAdventureWorks.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseSqlServer("Data Source=localhost;Initial Catalog=Gratis;Integrated Security=True");
});
// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddScoped<ITableService, TableServices>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(options => options
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    );

app.UseAuthorization();

app.MapControllers();

app.Run();
