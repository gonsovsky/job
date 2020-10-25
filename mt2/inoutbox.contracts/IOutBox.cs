using System.Collections.Generic;
using System.IO;

namespace InOutBox.Contracts
{
    public interface IOutBox
    {
        void Init(IConfig cfg);

        IOutItem Add(string extra = "");

        FileStream AddWrite(IOutItem item);

        void AddCommit(IOutItem item);

        void AddRollback(IOutItem item);

        event ItemDelegete OnAddItem;

        Stream Read(IOutItem item);

        void Send(IOutItem item);

        void Deliver(IOutItem item);

        void Fault(IOutItem item);

        IEnumerable<IOutItem> All();

        IEnumerable<IOutItem> Unsent();

        IEnumerable<IOutItem> Sent();

        void Clean(bool createnew = true);
    }

    public delegate void ItemDelegete(string queue, IOutItem item);
}
