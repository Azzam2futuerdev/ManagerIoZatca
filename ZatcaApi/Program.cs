using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ZatcaApi.Models;
using ZatcaApi.Services;
using ZatcaApi.Repositories;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using ZatcaApi.Helpers;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();
builder.Services.AddWindowsService();

builder.Services.Configure<JsonOptions>(options =>
         options.SerializerOptions.DefaultIgnoreCondition
   = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var (businessInfo, BusinessDataCustomField, gatewaySettings) = ConfigLoader.LoadConfiguration();

builder.Services.AddSingleton(businessInfo);
builder.Services.AddSingleton(BusinessDataCustomField);
builder.Services.AddSingleton(gatewaySettings);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=History.db"));

builder.Services.AddScoped<IRepository<ApprovedInvoice>, ApprovedInvoiceRepository>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IZatcaService, ZatcaService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Manager.io - Zatca Gateway API", Version = "v1" });
    c.MapType<GatewayRequestApi>(() => SwaggerDefaults.GetGatewayRequestApiSchema());
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowAllOrigins");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Manager.io - Zatca Gateway API v1");
});


app.MapGet("/config", (BusinessInfo businessInfo, BusinessDataCustomField businessDataCustomField, GatewaySetting gatewaySettings) =>
    Results.Json(new { BusinessInfo = businessInfo, BusinessDataCustomField = businessDataCustomField, GatewaySettings = gatewaySettings })

).WithTags("Zatca Api Gateway");


app.MapGet("/approved-invoice", async (IRepository<ApprovedInvoice> repository, int page = 1, int pageSize = 50) =>
{
    if (page < 1 || pageSize < 1)
    {
        return Results.BadRequest("Page and pageSize must be greater than zero.");
    }

    try
    {
        var invoiceLogs = await repository.GetPaged(page, pageSize);
        if (!invoiceLogs.Any() && page > 1)
        {
            return Results.NotFound();
        }
        return Results.Ok(invoiceLogs);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message);
    }
}).WithTags("Zatca Api Gateway");


//app.MapDelete("/approved-invoice/{id}", async (int id, AppDbContext context) =>
//{
//    var repository = new ApprovedInvoiceRepository(context);
//    var existingInvoiceLog = await repository.GetById(id);
//    if (existingInvoiceLog == null)
//    {
//        return Results.NotFound();
//    }

//    await repository.Delete(id);
//    return Results.Ok();

//}).WithTags("Zatca Api Gateway").WithName("Remove Invoice Log by Id");


app.MapPost("/invoice-reporting", async (GatewayRequestApi request, IZatcaService zatcaService) =>
{
    if (request == null)
    {
        return Results.BadRequest("Invalid JSON payload");
    }

    var (statusCode, resultContent) = await zatcaService.ReportInvoiceAsync(request);

    return Results.Ok(resultContent);

}).WithTags("Zatca eInvoice")
  .WithName("Reporting");


app.MapPost("/invoice-clearance", async (GatewayRequestApi request, IZatcaService zatcaService) =>
{
    if (request == null)
    {
        return Results.BadRequest("Invalid JSON payload");
    }

    var (statusCode, resultContent) = await zatcaService.ClearInvoiceAsync(request);

    return Results.Ok(resultContent);

}).WithTags("Zatca eInvoice")
  .WithName("Clearance");


app.MapPost("/compliance-invoice", async (GatewayRequestApi request, IZatcaService zatcaService) =>
{
    if (request == null)
    {
        return Results.BadRequest("Invalid JSON payload");
    }

    var (statusCode, resultContent) = await zatcaService.ComplianceInvoiceAsync(request);

    return Results.Ok(resultContent);

}).WithTags("Zatca eInvoice")
  .WithName("Compliance");

app.Run();
