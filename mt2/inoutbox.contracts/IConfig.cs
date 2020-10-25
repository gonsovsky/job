using System;
using System.Collections.Generic;
using System.Text;

namespace InOutBox.Contracts
{
    public interface IConfig
    {
        string StorageFolder { get; set; }
        string ConStr { get; set; }
        string DbFile { get; set; }
    }
}
