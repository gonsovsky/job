using System;

namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// варианты сохранения пакетных данных
    /// </summary>
    [Flags]
    public enum CommitOptions
    {
        None = 0,
        DeletePrevious = 1 << 0,
    }
}
