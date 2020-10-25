namespace InOutBox.Contracts
{
    public interface IStorageSvc
    {
        IOutBox GetOutBox(string queue, int priority);
    }
}
