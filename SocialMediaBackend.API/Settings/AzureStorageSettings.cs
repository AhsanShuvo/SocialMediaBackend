namespace SocialMediaBackend.API.Settings
{
    public class AzureStorageSettings
    {
        public string ConnectionString { get; set; }
        public string OriginalContainerName { get; set; }
        public string ProcessedContainerName { get; set; }
        public int UrlExpiresInMinute { get; set; }
    }
}
