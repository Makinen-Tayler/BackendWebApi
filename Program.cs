using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WEBAPI.DBContext;

var builder = WebApplication.CreateBuilder(args);


// Set up configuration to load settings from appsettings.json
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();


// Add services to the container.

// Add your DbContext to the DI container
builder.Services.AddDbContext<MyDBContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Enable CORS
app.UseCors(builder => builder
    .WithOrigins("http://localhost:4200") // Replace with the origin of your Angular application
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
);


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
