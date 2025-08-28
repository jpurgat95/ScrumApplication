using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
// Dodaj SignalR
builder.Services.AddSignalR();
// Dodanie po³¹czenia do bazy
builder.Services.AddDbContext<ScrumDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// dodajemy Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ScrumDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();
//Ciasteczko do logowania
builder.Services.ConfigureApplicationCookie(options =>
    {
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // tutaj ustaw czas "zapamiêtywania"
    options.LoginPath = "/Login";
    options.SlidingExpiration = true;
});
// Add services to the container.
builder.Services.AddRazorPages();
// Rejestracja repozytoriów
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();


var app = builder.Build();

// Mapowanie hubu
app.MapHub<UpdatesHub>("/updatesHub");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// Inicjalizacja ról i u¿ytkowników
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    await RoleInitializer.SeedRolesAsync(roleManager, userManager);
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
//Autentykacja - zwi¹zane z Identity
app.UseAuthentication();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
