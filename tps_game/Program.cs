using tps_game.Code;

// First setup the database
#if DEBUG
tps_game.Database.ResetDB();
#else
tps_game.Database.InitDB();
#endif

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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

app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        if (context.Request.Path.ToString().StartsWith("/game"))
        {
            // New game(s)
            await tps_game.Code.GameRouter.HandleWebSocketRequest(context);
        }
        else
        {
            // Old game
            await tps_game.Code.Old.WebSocketHandler.HandleWebSocketRequest(context);
        }
    }
    else
    {
        await next();
    }
});

app.Use(Static.CheckTokenMiddleware);

app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Home}/{action=Index}/{id?}");
    pattern: "{controller=Game}/{action=Snake}/{id?}");

app.Run();
