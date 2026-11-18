using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// CORS Policy එකතු කිරීම
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Database connection (SQLite) - මෙතන ලයින් එක කඩන්නේ නැතුව එක දිගට ලියන්න
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS Middleware එක (අනිවාර්යයෙන්ම app.UseAuthorization වලට උඩින් තිබිය යුතුය)
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
