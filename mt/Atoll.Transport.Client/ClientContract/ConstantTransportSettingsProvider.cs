namespace Atoll.Transport.Client.Contract
{
    public class ConstantTransportSettingsProvider : ITransportSettingsProvider
    {
        private readonly TransportSettings settings;

        public ConstantTransportSettingsProvider(TransportSettings settings)
        {
            this.settings = settings;
        }

        public TransportSettings GetSettings()
        {
            return this.settings;
        }
    }
}
