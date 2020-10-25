using System;
using System.Collections.Generic;
using System.Text;
using InOutBox.Contracts;

namespace InOutBox.Implementation
{
    public class Config: IConfig
    {
        public string StorageFolder { get; set; }
        public string ConStr { get; set; }
        public string DbFile { get; set; }
    }
}
