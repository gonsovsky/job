using System.IO;

namespace InOutBox.Contracts
{
    public interface IOutBox
    {
        int Add(string topic = "");

        FileStream Write(int messageId);

        void Commit(int messageId);

        void Rollback(int messageId);
    }

    public interface IOutBoxTransport
    {
        Stream Read(int messageId);

        void Send(int messageId);

        void Deliver(int messageId);
    }
}
