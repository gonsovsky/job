using System;
using System.Security.Cryptography;
#if NETSTANDARD2_0
#endif

namespace Coral.Atoll.Utils
{
    public static class HashHelper
    {
        public static string GetHashString(this HashAlgorithm hashAlg)
        {
            return BitConverter.ToString(hashAlg.Hash).Replace("-", "").ToLower();
        }
    }
}
