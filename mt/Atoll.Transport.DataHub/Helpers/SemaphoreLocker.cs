using System;
using System.Threading;
using System.Threading.Tasks;

namespace Atoll.Transport.DataHub
{
    public class SemaphoreLocker
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public async Task LockAsync(Func<Task> worker)
        {
            await this.semaphore.WaitAsync();
            try
            {
                await worker();
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }

}
