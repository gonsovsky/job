namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// Контракт провайдера получения идентификационных данных компьютера.
    /// </summary>
    public interface IComputerIdentityProvider
    {

        /// <summary>
        /// Получить идентификационные данные текущего компьютера.
        /// </summary>
        /// <returns>идентификационные данные текущего компьютера.</returns>
        ComputerIdentity GetIdentity();

    }
}
