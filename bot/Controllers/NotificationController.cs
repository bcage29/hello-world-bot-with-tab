using NotificationBot.Models;
using AdaptiveCards.Templating;
using Microsoft.AspNetCore.Mvc;
using Microsoft.TeamsFx.Conversation;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Options;
using System.Text;

namespace NotificationBot.Controllers
{
    [Route("api/notification")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly ConversationBot _conversation;

        private AppSettings _settings;
        private readonly string _adaptiveCardFilePath = Path.Combine(".", "Resources", "NotificationDefault.json");

        public NotificationController(ConversationBot conversation, IOptions<AppSettings> settings)
        {
            this._conversation = conversation;
            this._settings = settings.Value;
        }

        [HttpPost]
        [Route("conversation")]
        public async Task<ActionResult> PostConversationAsync(CancellationToken cancellationToken = default)
        {
            // Read adaptive card template
            var cardTemplate = await System.IO.File.ReadAllTextAsync(_adaptiveCardFilePath, cancellationToken);

            var pageSize = 100;
            string continuationToken = null;
            do
            {
                var pagedInstallations = await _conversation.Notification.GetPagedInstallationsAsync(pageSize, continuationToken, cancellationToken);
                continuationToken = pagedInstallations.ContinuationToken;
                var installations = pagedInstallations.Data;
                foreach (var installation in installations)
                {
                    // Build and send adaptive card
                    var cardContent = new AdaptiveCardTemplate(cardTemplate).Expand
                    (
                        new NotificationDefaultModel
                        {
                            Title = "New Event Occurred!",
                            AppName = "Contoso App Notification",
                            Description = $"This is a sample http-triggered notification to {installation.Type}",
                            NotificationUrl = "https://aka.ms/teamsfx-notification-new",
                        }
                    );
                    await installation.SendAdaptiveCard(JsonConvert.DeserializeObject(cardContent), cancellationToken);
                }

            } while (!string.IsNullOrEmpty(continuationToken));

            return Ok();
        }

        [HttpPost]
        [Route("activity")]
        public async Task<ActionResult> PostActivityAsync(CancellationToken cancellationToken = default)
        {
            appId = string.Empty;
            //await GetInstalledAppList("82e79299-b14a-41b3-a23b-dec45825d069");
            await SendNotification("82e79299-b14a-41b3-a23b-dec45825d069", "ODJlNzkyOTktYjE0YS00MWIzLWEyM2ItZGVjNDU4MjVkMDY5IyMzNzdhYjY1Mi1lNWQxLTRjNDMtYTE3NS1hMzYyYjY0OWZlZTI=");
            return Ok();
        }

        private string appId = string.Empty;

        private async Task GetInstalledAppList(string reciepientUserId)
        {
            // Replace with your Graph API endpoint and access token
            string graphApiEndpoint = $"https://graph.microsoft.com/v1.0/users/{reciepientUserId}/teamwork/installedApps/?$expand=teamsApp&$filter=teamsApp/externalId eq '{_settings.TEAMS_APP_ID}'";

            var accessToken = await GetToken();

            // Create an HttpClient instance
            using (HttpClient client = new HttpClient())
            {
                // Set the base address for the Graph API
                client.BaseAddress = new Uri(graphApiEndpoint);

                // Set the authorization header with the access token
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                try
                {
                    // Make a GET request to retrieve user data
                    HttpResponseMessage response = await client.GetAsync(graphApiEndpoint);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read and display the response content
                        string responseBody = await response.Content.ReadAsStringAsync();

                        var responseData = JsonConvert.DeserializeObject<ResponseData>(responseBody);
                        var installedAppList = responseData.Value;

                        if (installedAppList.Count == 1)
                        {
                            appId = installedAppList[0].Id;
                        }

                        // foreach(var element in installedAppList)
                        // {
                        //     if (element.TeamsAppDefinition.DisplayName == "hello-world-bot-with-tablocal")
                        //     {
                        //         appId = element.Id;
                        //     }
                        // };
                    
                        if(appId != null)
                        {
                            await SendNotification(reciepientUserId, appId);
                            //return "Message sent successfully";
                        }
                        else
                        {
                            //return "App not installed for the user";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        //return "Error occured";
                    }
                }
                catch (Exception ex)
                {          
                    Console.WriteLine($"Error: {ex.Message}");
                    //return "Error occured"+ ex.Message;
                }
            }
        }

        /// <summary>
        /// Send activity feed notification to user.
        /// </summary>
        /// <param name="reciepientUserId"> Id of the user whom notification is to be sent</param>
        /// <param name="appId">App id for rsc app.</param>
        /// <returns></returns>
        private async Task<string> SendNotification(string reciepientUserId, string appId)
        {
            // Set your Graph API endpoint and access token
            string graphApiEndpoint = $"https://graph.microsoft.com/beta/users/{reciepientUserId}/teamwork/sendActivityNotification";

            var accessToken = await GetToken();

            // Create a JSON payload for the activity feed notification
            // json payload to use with template defined in manifest
            string jsonPayload = @"{
             ""topic"": {
                ""source"": ""entityUrl"",
                ""value"": ""https://graph.microsoft.com/beta/users/" + reciepientUserId + "/teamwork/installedApps/" + appId + "/index0" + @"""
            },
            ""activityType"": ""taskCreated"",
            ""previewText"": {
                ""content"": ""New Task Created""
            },
            ""templateParameters"": [{
                ""name"": ""taskName"",
                ""value"": ""test""
                }]
            }";

            string jsonPayloadCustomTopic = @"{
             ""topic"": {
                ""source"": ""text"",
                ""value"": ""test"",
                ""webUrl"": ""https://teams.microsoft.com/l/entity/" + _settings.TEAMS_APP_ID + "/index0?tenantId=" + _settings.TENANT_ID + "&webUrl=https://localhost:53000" + @"""
            },
            ""activityType"": ""taskCreated"",
            ""previewText"": {
                ""content"": ""New Task Created""
            },
            ""templateParameters"": [{
                ""name"": ""taskName"",
                ""value"": ""test""
                }]
            }";

            // json payload to use with system default template
            string jsonPayloadSystemDefault = @"{
            ""topic"": {
                ""source"": ""text"",
                ""value"": ""test"",
                ""webUrl"": ""https://teams.microsoft.com/l/entity/" + _settings.TEAMS_APP_ID + "/index0?tenantId=" + _settings.TENANT_ID + "&webUrl=https://localhost:53000&label=hello" + @"""
            },
            ""activityType"": ""systemDefault"",
            ""previewText"": {
                ""content"": ""Take a break""
            },
            ""recipient"": {
                ""@odata.type"": ""microsoft.graph.aadUserNotificationRecipient"",
                ""userId"": ""82e79299-b14a-41b3-a23b-dec45825d069""
            },
            ""templateParameters"": [{
                ""name"": ""systemDefaultText"",
                ""value"": ""You need to take a short break""
                }]
            }";
            
            using (HttpClient httpClient = new HttpClient())
            {
                // Set the base URL for the Graph API
                httpClient.BaseAddress = new Uri(graphApiEndpoint);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                try
                {
                    // Create a POST request with the JSON payload
                    HttpResponseMessage response = await httpClient.PostAsync(graphApiEndpoint, new StringContent(jsonPayloadCustomTopic, Encoding.UTF8, "application/json"));

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Parse and print the response content
                        string content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(content);
                        return "true";
                    }
                    else
                    {
                        var c = response.Content.ReadAsStringAsync();
                        Console.WriteLine(c?.Result);
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return "Notification sent";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    return "Notification failed";
                }
            }
        }

        /// <summary>
        /// Get Token for given tenant.
        /// </summary>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        private async Task<string> GetToken()
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(_settings.TeamsFx.Authentication.ClientId)
                                                  .WithClientSecret(_settings.TeamsFx.Authentication.ClientSecret)
                                                  .WithAuthority($"https://login.microsoftonline.com/{_settings.TENANT_ID}")
                                                  .WithRedirectUri("https://daemon")
                                                  .Build();

            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            return result.AccessToken;
        }
    }

    /// <summary>
    /// Class for graph api response data
    /// </summary>
    public class ResponseData
    {
        /// <summary>
        /// List of installed app
        /// </summary>
        public List<AppData> Value { get; set; }
    }

    /// <summary>
    /// Class for installed app data
    /// </summary>
    public class AppData
    {
        /// <summary>
        /// Id of the installed app
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Teams app defination of the installed app
        /// </summary>
        public AppDefination TeamsAppDefinition { get; set; }
    }

    /// <summary>
    /// Teams app defination of the installed app
    /// </summary>
    public class AppDefination
    {
        /// <summary>
        /// Id of the installed app
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the installed app
        /// </summary>
        public string DisplayName { get; set; }
    }
}
