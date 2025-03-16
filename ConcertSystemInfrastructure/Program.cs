using ConcertSystemInfrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ������� ��� ��� ��� ���������� �� ���� �����
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ConcertTicketSystemContext>(options =>
    options.UseSqlServer(connectionString));

// ���� ������
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ������������ middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Concerts}/{action=Index}/{id?}");

app.Run();