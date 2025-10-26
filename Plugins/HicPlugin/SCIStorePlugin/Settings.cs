namespace SCIStorePlugin
{
    /// <summary>
    /// Application settings for SCI Store Plugin
    /// </summary>
    internal sealed class Settings : System.Configuration.ApplicationSettingsBase
    {
        private static readonly Settings defaultInstance = ((Settings)Synchronized(new Settings()));

        public static Settings Default => defaultInstance;

        public string FriendlyName { get; set; }
        public string SystemCode { get; set; }
        public string SystemLocation { get; set; }
        public string UserName { get; set; }
    }
}