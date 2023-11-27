using NotificationBot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.TeamsFx.Conversation;
using NotificationBot.Bots;
using Microsoft.TeamsFx.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient("WebClient", client => client.Timeout = TimeSpan.FromSeconds(600));
builder.Services.AddHttpContextAccessor();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

//builder.Services.AddOptions<AppSettings>();
builder.Services
    .AddOptions<AppSettings>()
    .BindConfiguration(nameof(AppSettings))
    .ValidateDataAnnotations();

// Prepare Configuration for ConfigurationBotFrameworkAuthentication
var config = builder.Configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();
builder.Configuration["MicrosoftAppType"] = "MultiTenant";
builder.Configuration["MicrosoftAppId"] = config.BOT_ID;
builder.Configuration["MicrosoftAppPassword"] = config.BOT_PASSWORD;

// Create the Bot Framework Authentication to be used with the Bot Adapter.
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// Create the Cloud Adapter with error handling enabled.
// Note: some classes expect a BotAdapter and some expect a BotFrameworkHttpAdapter, so
// register the same adapter instance for all types.
builder.Services.AddSingleton<CloudAdapter, AdapterWithErrorHandler>();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter>(sp => sp.GetService<CloudAdapter>()!);
builder.Services.AddSingleton<BotAdapter>(sp => sp.GetService<CloudAdapter>()!);

// Create the Bot Framework Adapter with error handling enabled.                                        
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// Create the storage we'll be using for User and Conversation state, as well as Single Sign On.
// (Memory is great for testing purposes.)
builder.Services.AddSingleton<IStorage, MemoryStorage>();

// Create the User state. (Used in this bot's Dialog implementation.)
builder.Services.AddSingleton<UserState>();

// Create the Conversation state. (Used by the Dialog system itself.)
builder.Services.AddSingleton<ConversationState>();

// The Dialog that will be run by the bot.
builder.Services.AddSingleton<SsoDialog>();

// Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
builder.Services.AddTransient<IBot, TeamsSsoBot<SsoDialog>>();

builder.Services.AddOptions<BotAuthenticationOptions>().Configure(options =>
{
  options.ClientId = config.TeamsFx.Authentication.ClientId;
  options.ClientSecret = config.TeamsFx.Authentication.ClientSecret;
  options.OAuthAuthority = config.TeamsFx.Authentication.OAuthAuthority;
  options.ApplicationIdUri = config.TeamsFx.Authentication.ApplicationIdUri;
  options.InitiateLoginEndpoint = config.TeamsFx.Authentication.Bot.InitiateLoginEndpoint;
});

builder.Services.AddSingleton(sp =>
{
  var options = new ConversationOptions()
  {
    Adapter = sp.GetService<CloudAdapter>(),
    Notification = new NotificationOptions
    {
        BotAppId = config.BOT_ID,
        //Store = CustomStorageStore
        //This needs to be an implementation of IConversationReferenceStore
    }
  };

  return new ConversationBot(options);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();