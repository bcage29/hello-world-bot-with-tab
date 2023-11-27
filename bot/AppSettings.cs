using Microsoft.TeamsFx.Configuration;

namespace NotificationBot
{
    public class TeamsFxOptions
    {
        public AuthenticationOptions Authentication { get; set; }
    }

    public class AppSettings
    {
        public string BOT_ID { get; set; }
        public string BOT_PASSWORD { get; set; }
        public string TENANT_ID { get; set; }
        public string TEAMS_APP_ID { get; set; }
        public TeamsFxOptions TeamsFx { get; set; }
    }
}
