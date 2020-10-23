using System;

namespace ClassLibrary1.Transport
{
    public static class TransportConstants
    {
        public const string MongoDatabaseName = "AtollDb";
        public const string GlobalSettingTable = "settings";
        public const string PacketsPartsTable = "packetparts";
        public const string LeaseLockTable = "dblease";


        public const string StorageTokenKey = "StorageToken";
        public const string DbLeaseId = "DbLease";



        /// <summary>
        /// имя свойства 
        /// </summary>
        public const string FormDataPacketsProp = "packets";
        /// <summary>
        /// имя свойства 
        /// </summary>
        public const string FormDataConfigurationProp = "confs";

        /// <summary>
        /// Формат данных при котором в запрос будет записываться FormData
        /// </summary>
        public const string RequestFormDataFormat = "formdata";

        //
        public static TimeSpan DefaultCommonErrorRetryTimeout = TimeSpan.FromMinutes(10);

        public static TimeSpan DefaultUpdateStaticConfigTimeout = TimeSpan.FromSeconds(10);
        public static TimeSpan DefaultUpdateDbTokenTimeout = TimeSpan.FromSeconds(10);
        public static TimeSpan DefaultCommonServerErrorsTimeout = TimeSpan.FromMinutes(3);

        public static TimeSpan DefaultCollectMinPacketSizeTimeout = TimeSpan.FromMinutes(5);
        public static TimeSpan DefaultConfigurationUpdateTimeout = TimeSpan.FromMinutes(5);

        // 5 kb
        public static int DefaultMinClientPacketSize = 5 * 1024;
        // 16 mb
        public static int DefaultMaxClientPacketSize = 16 * 1024 * 1024;

        // 5 kb
        public static int DefaultMinResponsePacketSize = 5 * 1024;
        // 16 mb
        public static int DefaultMaxResponsePacketSize = 16 * 1024 * 1024;
    }
}
