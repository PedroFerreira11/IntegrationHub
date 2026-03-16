var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var integrationHubApiBaseUrl = builder.Configuration["ServiceUrls:IntegrationHubApi"]
    ?? throw new InvalidOperationException("Missing configuration: ServiceUrls:IntegrationHubApi");

builder.Services.AddHttpClient("MyApi", client =>
{
    client.BaseAddress = new Uri($"{integrationHubApiBaseUrl.TrimEnd('/')}/api/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
