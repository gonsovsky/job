using InOutBox.Contracts;

namespace InOutBox.Implementation
{
    public class OutItem: IOutItem
    {
        public int Id { get; set; }
        public int Priority { get; set; }
    }
}
