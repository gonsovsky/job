﻿namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// Ограничение на размер передаваемых пакетов между сервером и агентом
    /// </summary>
    public class SendMessageSizeLimits
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }
}
