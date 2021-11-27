using System;

namespace Quick.Rpc
{
    internal class ClientServiceProxyInfo : IDisposable
    {
        internal ClientServiceProxyInfo(Type serviceType, object serviceToken)
        {
            ServiceToken = serviceToken;
            ServiceType = serviceType;
        }
        public Type ServiceType { get; }
        public object ServiceToken { get; }
        public object ServiceProxy { get; set; }
        public RpcInterceptor Interceptor { get; set; }

        public void Dispose()
        {
            Interceptor?.Dispose();
        }
    }
}
