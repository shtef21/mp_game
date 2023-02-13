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

app.Use(async (context, next) =>
{
    string requestPath = context.Request.Path.ToString().ToLower();
    bool tokenValid = false;

    // Check token
    string? userToken = context.Request.Cookies["token"];
    if (userToken != null)
    {
        tokenValid = tps_game.Database.SnakeCheckToken(userToken);
    }

    if (tokenValid == false)
    {
        // Token not found or invalid

        if (requestPath == "/user/login")
        {
            // This is a path for user login
            await next();
        }
        else if (requestPath != "/home/login")
        {
            // No token, redirect to login
            context.Response.Redirect("/home/login");
        }
        else
        {
            await next();
        }
    }
    else
    {
        await next();
    }
});

app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Home}/{action=Index}/{id?}");
    pattern: "{controller=Game}/{action=Snake}/{id?}");

app.Run();
