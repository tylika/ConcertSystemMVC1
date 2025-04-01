using ConcertSystemInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ConcertSystemDomain.Model;

var builder = WebApplication.CreateBuilder(args);

// ������ �������� ���� �����
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ConcertTicketSystemContext>(options =>
    options.UseSqlServer(connectionString));

// ������ Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ConcertTicketSystemContext>()
    .AddDefaultTokenProviders();

// ������������ ��������������
builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ����������� ����� � �����
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // ���������� ����������� �����
    await InitializeRolesAndAdminAsync(roleManager, userManager);
}

app.Run();

// ���������� ������� ��� ����������� ����� � �����
async Task InitializeRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
{
    string[] roleNames = { "Admin", "Viewer" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var adminEmail = "admin@concertsystem.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = "Admin User" };
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}