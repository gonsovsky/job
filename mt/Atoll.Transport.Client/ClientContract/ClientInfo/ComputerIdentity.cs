namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// Контейнер идентификационной информации о компьютере.
    /// </summary>
    public sealed class ComputerIdentity
    {

        /// <summary>
        /// Имя компьютера.
        /// </summary>
        public readonly string ComputerName;

        /// <summary>
        /// Имя домена компьютера.
        /// </summary>
        public readonly string DomainName;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="computerName">имя компьютера.</param>
        /// <param name="domainName">имя домена компьютера.</param>
        public ComputerIdentity(string computerName, string domainName)
        {
            this.ComputerName = computerName;
            this.DomainName = domainName;
        }

    }
}
