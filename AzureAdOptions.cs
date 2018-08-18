namespace Microsoft.AspNetCore.Authentication
{
    public class AzureAdOptions
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Instance { get; set; }

        public string Domain { get; set; }

        public string DirectoryId { get; set; }

        public string CallbackPath { get; set; }

        public bool SaveToken { get; set; }
    }
}
