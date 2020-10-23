using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InOutBox.Demo
{
    public static class Config
    {
        public static readonly string StorageFolder = @"C:/_temp/";
        public static readonly string TransportFolder = Path.Combine(StorageFolder, "transport");
        public static readonly string TransportUrl = "http://192.168.100.184:5001/dhu/transport/exchange";
        public static readonly string QueueName = "smbios";
        public static readonly string DbFile = Path.Combine(StorageFolder, "outbox.db3");
        public static readonly string ConStr = $@"Data Source={DbFile};Version=3;New=False;Compress=True;";
    }
}
