namespace Atoll.Transport.DataProcessing
{
    /// <summary>
    /// Поиск и построение контейнеров с обработчиками событий.
    /// </summary>
    public interface IProcessorContainerFactory
    {

        string Id { get; }

        string CircuitName { get; }

        ProcessorContainer GetProcessorContainer();

    }
}
