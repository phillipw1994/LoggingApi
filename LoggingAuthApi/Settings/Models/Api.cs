namespace LoggingAuthApi.Settings.Models
{
    public class Api
    {
        public string Auth0Domain { get; set; }
        public string Auth0Audience { get; set; }
        public string Auth0Issuer { get; set; }

        public string PawsApplicationClientId { get; set; }
        public string PawsApplicationClientSecret { get; set; }
        public string PawsApplicationDisplayName { get; set; }
        public string CorsOrigins { get; set; }
        public string CorsOriginsLocalHost { get; set; }
        public string CorsOriginsScanner { get; set; }
    }
}