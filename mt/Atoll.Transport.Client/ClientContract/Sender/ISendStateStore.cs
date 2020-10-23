
namespace Atoll.Transport.Client.Contract
{
    public interface ISendStateStore
    {

        SendState Get();
        void ConfigurationsUpdated();
        void CheckedPacketsSize();

    }
}
