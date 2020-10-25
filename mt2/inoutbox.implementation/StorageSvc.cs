using InOutBox.Contracts;

namespace InOutBox.Implementation
{
    public class StorageSvc: IStorageSvc
    {
        public IOutBox GetOutBox(string queue, int priority)
        {
            return new OutBox(queue, priority);
        }
    }
}
