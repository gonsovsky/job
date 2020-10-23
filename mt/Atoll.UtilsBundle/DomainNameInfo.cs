using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

namespace Coral.Atoll.Utils
{
    /// <summary>
    /// Статический класс, содержащий логику получения имени домена компьютера для различных целевых платформ. 
    /// </summary>
    public static class DomainNameInfo
    {


        #region Constants

        /// <summary>
        /// Максимальная длина имени домена.
        /// </summary>
        private const int MaxDomainNameLength = 260;

        /// <summary>
        /// Строка, используемая в качестве имени домена, если текущий компьютер не включен в домен.
        /// </summary>
        public const string NoDomainName = ".";

        #endregion

        #region Nested Classes

        /// <summary>
        /// Статический класс, содержащий Interop-методы для ОС Windows.
        /// </summary>
        private static class Win32Interop
        {

            // ReSharper disable UnusedMember.Global
            // ReSharper disable UnusedMember.Local
            // ReSharper disable InconsistentNaming

            /// <summary>
            /// Перечисление типов получаемых имен.
            /// </summary>
            public enum COMPUTER_NAME_FORMAT
            {

                ComputerNameNetBIOS,
                ComputerNameDnsHostname,
                ComputerNameDnsDomain,
                ComputerNameDnsFullyQualified,
                ComputerNamePhysicalNetBIOS,
                ComputerNamePhysicalDnsHostname,
                ComputerNamePhysicalDnsDomain,
                ComputerNamePhysicalDnsFullyQualified

            }

            /// <summary>
            /// WinAPI-функция GetComputerNameEx.
            /// </summary>
            /// <remarks>
            ///https://docs.microsoft.com/en-us/windows/desktop/api/sysinfoapi/nf-sysinfoapi-getcomputernameexa
            /// </remarks>
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            [ResourceExposure(ResourceScope.None)]
            [SecurityCritical]
            public static extern bool GetComputerNameEx([In] COMPUTER_NAME_FORMAT nameType, [In, Out, MarshalAs(UnmanagedType.LPTStr)]StringBuilder lpBuffer, [In, Out] ref int lpnSize);

            // ReSharper restore InconsistentNaming
            // ReSharper restore UnusedMember.Local
            // ReSharper restore UnusedMember.Global
        }

#if NETSTANDARD2_0

        /// <summary>
        /// Статический класс, содержащий Interop-методы для ОС *nix.
        /// </summary>
        private static class UnixInterop
        {

            /// <summary>
            /// LIBC-функция GetDomainName.
            /// </summary>
            /// <remarks>
            /// https://www.freebsd.org/cgi/man.cgi?query=getdomainname
            /// </remarks>
            [DllImport("System.Native", EntryPoint = "SystemNative_GetDomainName", SetLastError = true)]
            public static extern unsafe int GetDomainName(byte* buffer, int length);

        }

#endif

        #endregion

        #region Public Methods
        
        /// <summary>
        /// Получить имя домена компьютера методом TryGet. 
        /// </summary>
        /// <param name="domainName">имя домена.</param>
        /// <param name="errorMessage">сообщение об ошибке.</param>
        /// <returns></returns>
        public static bool TryGetDomainName(out string domainName, out string errorMessage)
        {

            try
            {
#if NET40 

                // В варианте NET 4.0 - всегда используем Windows-версию.
                return TryGetDomainNameWindows(out domainName, out errorMessage);

#elif NETSTANDARD2_0

                // В варианте NET Standard - используем либо Windows-, либо Unix-версию.
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? TryGetDomainNameWindows(out domainName, out errorMessage)
                    : TryGetDomainNameUnix(out domainName, out errorMessage);

#endif
            }
            catch (Exception ex)
            {
                errorMessage = string.Concat("Exception occured while getting domain name: ", ex.ToString());
                domainName = null;
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Windows-версия получения имени домена.
        /// </summary>
        [SecuritySafeCritical]
        private static bool TryGetDomainNameWindows(out string domainName, out string errorMessage)
        {
            var domainNameLength = MaxDomainNameLength;
            var domainNameBuilder = new StringBuilder(MaxDomainNameLength);
            if (!Win32Interop.GetComputerNameEx(Win32Interop.COMPUTER_NAME_FORMAT.ComputerNameDnsDomain, domainNameBuilder, ref domainNameLength))
            {
                errorMessage = string.Concat("WinAPI function [GetComputerNameEx] failed with code: ", Marshal.GetLastWin32Error());
                domainName = null;
                return false;
            }

            if (domainNameLength < 1)
            {
                domainName = NoDomainName;
                errorMessage = null;
                return true;
            }

            domainName = domainNameBuilder.ToString(0, domainNameLength);
            errorMessage = null;
            return true;
        }

#if NETSTANDARD2_0

        /// <summary>
        /// *nix-версия получения имени домена.
        /// </summary>
        /// <remarks>
        /// Проверена в том числе на OS X.
        /// Реализация взята из исходников самого .NET Core.
        /// </remarks>
        [SecuritySafeCritical]
        private static bool TryGetDomainNameUnix(out string domainName, out string errorMessage)
        {
            unsafe
            {
                var domainNameBuffer = stackalloc byte[MaxDomainNameLength];
                if (UnixInterop.GetDomainName(domainNameBuffer, MaxDomainNameLength) != 0)
                {
                    // This should never happen.  According to the man page,
                    // the only possible errno for getdomainname is ENAMETOOLONG,
                    // which should only happen if the buffer we supply isn't big
                    // enough, and we're using a buffer size that the man page
                    // says is the max for POSIX (and larger than the max for Linux).

                    domainName = null;
                    errorMessage = "GetDomainName function failed (POSIX violation).";
                    return false;
                }

                domainName = Marshal.PtrToStringAnsi((IntPtr)domainNameBuffer);

                if (string.IsNullOrWhiteSpace(domainName))
                    domainName = NoDomainName;

                errorMessage = null;
                return true;
            }

        } 

#endif

        #endregion

    }
}
