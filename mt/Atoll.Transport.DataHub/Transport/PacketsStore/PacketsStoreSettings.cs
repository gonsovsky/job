namespace Atoll.Transport.DataHub
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
