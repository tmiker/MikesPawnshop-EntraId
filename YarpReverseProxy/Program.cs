using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAuthentication("Bearer")
.AddJwtBearer("Bearer", options =>
{
    options.Authority = builder.Configuration["AZURE_CREDENTIALS_AUTHORITY"];       //  "https://login.microsoftonline.com/{tenantId}/v2.0";
    options.Audience = builder.Configuration["AZURE_CREDENTIALS_CLIENT_ID"];        // "{clientId}";
    options.TokenValidationParameters.ValidateAudience = true;
});

builder.Services.AddAuthorization();


//// ADD RATE LIMITING POLICIES 
/// Rate Limiting Policies can be global or named - named are specified by endpoint or page

//// 1. Global Rate Limiting Policy that permits 10 requests per minute by user (identity) or globally:
//builder.Services.AddRateLimiter(options =>
//{
//    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
//        RateLimitPartition.GetFixedWindowLimiter(
//            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
//            factory: partition => new FixedWindowRateLimiterOptions
//            {
//                AutoReplenishment = true,
//                PermitLimit = 10,
//                QueueLimit = 0,
//                Window = TimeSpan.FromMinutes(1)
//            }));
//});

// 2. Named Rate Limiting Policy to be added to specific endpoints or globally (see yarp_notes.txt):
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("BasicRateLimitingPolicy", opt =>
    {
        opt.PermitLimit = 4;
        opt.Window = TimeSpan.FromSeconds(12);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

//// ADD REVERSE PROXY
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetRequiredSection("YarpProxySettings"));

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRouting();

app.UseAuthorization();

//// GLOBAL RATE LIMITING MIDDLEWARE
//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllers().RequireRateLimiting("BasicRateLimitingPolicy");
//});

app.MapControllers();

app.MapReverseProxy();  // .RequireAuthorization();  // this would require authentication for all proxied requests, which is not desired - need to add authentication to specific endpoints 

app.Run();