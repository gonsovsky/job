namespace InOutBox.Contracts
{
    public interface IStorage
    {
        IOutBox GetOutBox(string queue);
    }
}
