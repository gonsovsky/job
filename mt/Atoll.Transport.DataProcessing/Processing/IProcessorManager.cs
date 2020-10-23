namespace Atoll.Transport.DataProcessing
{
    public interface IProcessorManager
    {
        string Id { get; }

        string CircuitName { get; }

        bool IsEnabled { get; }

        IProcessorContainerFactory GetProcessorContainerFactory();
    }
}
