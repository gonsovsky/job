using System;
#if NETSTANDARD2_0
#endif

namespace ClassLibrary1.Transport
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
