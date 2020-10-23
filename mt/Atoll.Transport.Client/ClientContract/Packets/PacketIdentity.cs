using System;
using System.Collections.Generic;

namespace Atoll.Transport.Client.Contract
{
    public class PacketIdentity : IEquatable<PacketIdentity>
    {
        /// <summary>
        /// Ид пакета (сейчас guid)
        /// </summary>
        public string PacketId { get; private set; }

        /// <summary>
        /// Для определения порядка создания пакетов и порядка их отправки
        /// </summary>
        public int OrderValue { get; private set; }

        public PacketIdentity(string packetId, int orderValue)
        {
            this.PacketId = packetId;
            this.OrderValue = orderValue;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PacketIdentity);
        }

        public bool Equals(PacketIdentity other)
        {
            return other != null &&
                   PacketId == other.PacketId &&
                   OrderValue == other.OrderValue;
        }

        public override int GetHashCode()
        {
            var hashCode = 1741777648;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PacketId);
            hashCode = hashCode * -1521134295 + OrderValue.GetHashCode();
            return hashCode;
        }
    }
}
