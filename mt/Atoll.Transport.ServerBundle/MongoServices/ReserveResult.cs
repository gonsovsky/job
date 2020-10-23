using System;

namespace Atoll.Transport.ServerBundle
{
    public class ReserveResult
    {
        public bool IsSuccess { get; set; }
        public AcquiredLockServices ReservedServices { get; set; }

        public static ReserveResult SuccessResult(AcquiredLockServices acquiredLockServices)
        {
            if (acquiredLockServices == null)
            {
                throw new ArgumentNullException("acquiredLockServices");
            }

            if (acquiredLockServices == null)
            {
                throw new ArgumentNullException("acquiredLockServices");
            }

            return new ReserveResult
            {
                IsSuccess = true,
                ReservedServices = acquiredLockServices,
            };
        }

        public static ReserveResult FailResult()
        {
            return new ReserveResult
            {
                IsSuccess = false,
            };
        }

        public static ReserveResult FromValueResult(bool isSuccess, AcquiredLockServices acquiredLockServices)
        {
            if (acquiredLockServices == null)
            {
                throw new ArgumentNullException("acquiredLockServices");
            }

            return new ReserveResult
            {
                IsSuccess = true,
                ReservedServices = acquiredLockServices,
            };
        }

    }

}
