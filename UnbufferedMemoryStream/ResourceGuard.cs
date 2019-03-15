using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UnbufferedMemoryStream
{
    class ResourceGuard<T>
    {

        public void RegisterResource(T resource)
        public IDisposable LockResource(T resource, ResourceLock resourceLock)
        {

        }

        public Task<IDisposable> LockResourceAsync(T resource, ResourceLock resourceLock)


    }

    private struct ResourceSubscription : IDisposable
    {

    }

    private struct ResourceRegistration<T>
    {
        private int _maximumReadSubscriptions;
        private int _maximumWriteSubscriptions;

    }

    internal enum ResourceLock
    {
        Read,
        ReadWrite
    }
}
