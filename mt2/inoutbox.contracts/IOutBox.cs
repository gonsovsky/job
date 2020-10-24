using System.Collections.Generic;
using System.IO;

namespace InOutBox.Contracts
{

    public interface IOutBox
    {
        int Add(string extra = "");

        FileStream AddWrite(int itemId);

        void AddCommit(int itemId);

        void AddRollback(int itemId);

        event ItemDelegete OnNewItem;

        Stream Read(int itemId);

        void Send(int itemId);

        void Deliver(int itemId);

        void Fault(int itemId);

        IEnumerable<int> All();

        IEnumerable<int> Unsent();

        IEnumerable<int> Sent();
    }

    public delegate void ItemDelegete(string queue, int itemId);
}
