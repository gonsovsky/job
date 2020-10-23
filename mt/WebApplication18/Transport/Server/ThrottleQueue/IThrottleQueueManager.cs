namespace WebApplication18.Transport
{
    public interface IThrottleQueueManager
    {
        bool TryAccept(string id, ThrottleParams throttleParams);
        void Release(string id);
    }
}
