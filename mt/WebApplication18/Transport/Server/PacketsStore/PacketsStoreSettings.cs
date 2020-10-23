namespace WebApplication18.Transport
{
    public class PacketsStoreSettings
    {
        public string PacketsDirectory { get; set; }

        public PacketsStoreSettings(string pktsDir)
        {
            this.PacketsDirectory = pktsDir;
        }
    }

}
