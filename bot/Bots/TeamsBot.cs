using AdaptiveCards;
using AdaptiveCards.Templating;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using NotificationBot.Models;
using Newtonsoft.Json;

namespace NotificationBot.Bots
{
    /// <summary>
    /// Teams Bot Handler 
    /// </summary>
    public class TeamsBot : TeamsActivityHandler
    {
        private readonly string _adaptiveCardFilePath = Path.Combine(".", "Resources", "NotificationDefault.json");

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            turnContext.Activity.RemoveRecipientMention();
            var text = turnContext.Activity.Text.Trim().ToLower();

            // Read adaptive card template
            var cardTemplate = await System.IO.File.ReadAllTextAsync(_adaptiveCardFilePath, cancellationToken);


            // Build and send adaptive card
            var cardContent = new AdaptiveCardTemplate(cardTemplate).Expand
            (
                new NotificationDefaultModel
                {
                    Title = "New Event Occurred!",
                    AppName = "Contoso App Notification",
                    Description = $"This is a sample http-triggered notification to .Type",
                    NotificationUrl = "https://aka.ms/teamsfx-notification-new",
                }
            );           
            var att = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardContent),
            };

            var msg = MessageFactory.Attachment(att);
            msg.TeamsNotifyUser();
            await turnContext.SendActivityAsync(msg, cancellationToken);
        }
    }
}
