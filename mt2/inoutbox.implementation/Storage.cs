using InOutBox.Contracts;

namespace InOutBox.Implementation
{
    public class Storage: IStorage
    {
        public IOutBox GetOutBox(string queue)
        {
            return new OutBox(queue);
        }
    }
}
