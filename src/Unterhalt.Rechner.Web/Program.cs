using Unterhalt.Rechner.Domain;
using Unterhalt.Rechner.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<IDusseldorfTable, DusseldorfTable2025>();
builder.Services.AddSingleton<IncomeCalculator>();
builder.Services.AddTransient<SupportCalculator>();
builder.Services.AddScoped<CalculationState>();
builder.Services.AddSingleton<TaxNoticeOcrService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
