using InOutBox.Contracts;

namespace InOutBox.Implementation
{
    public class Storage: IStorage
    {
        public IOutBox GetOutBox(string queue, int priority)
        {
            return new OutBox(queue, priority);
        }
    }
}
