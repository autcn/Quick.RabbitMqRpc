using System;

namespace Quick.Rpc
{
    internal class ClientServiceProxyInfo : IDisposable
    {
        internal ClientServiceProxyInfo(Type serviceType, string serviceCollectionName)
        {
            ServiceCollectionName = serviceCollectionName;
            ServiceType = serviceType;
        }
        public Type ServiceType { get; }
        public string ServiceCollectionName { get; }
        public object ServiceProxy { get; set; }
        public RpcInterceptor Interceptor { get; set; }

        public void Dispose()
        {
            Interceptor?.Dispose();
        }
    }
}
