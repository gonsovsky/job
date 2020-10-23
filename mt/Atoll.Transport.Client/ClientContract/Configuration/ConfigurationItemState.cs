using Atoll.Transport.Contract;

namespace Atoll.Transport.Client.Contract
{
    public class ConfigurationState
    {
        public string ProviderKey { get; set; }
        public string Token { get; set; }
        public long? StartPosition { get; set; }
        public bool IsCompleted { get; set; }
    }

    public static class ConfigurationRequestExtensions
    {
        public static ConfigurationRequestDataItem ToRequestItem(this ConfigurationState state)
        {
            return new ConfigurationRequestDataItem
            {
                ProviderKey = state.ProviderKey,
                Token = state.Token,
                StartPosition = state.StartPosition,
                IsCompleted = state.IsCompleted,
            };
        }
    }
}
