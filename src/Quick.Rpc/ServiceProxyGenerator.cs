using Castle.DynamicProxy;
using System;
using System.Collections.Concurrent;

namespace Quick.Rpc
{
    /// <summary>
    /// Generate service proxy for client RPC calls.
    /// </summary>
    public class ServiceProxyGenerator : IDisposable
    {
        /// <summary>
        /// Create an instance of ServiceProxyGenerator
        /// </summary>
        /// <param name="rpcTransfer">A class instance which implemented IRpcClient interface.</param>
        public ServiceProxyGenerator(IRpcClient rpcTransfer)
        {
            _rpcTransfer = rpcTransfer;
        }
        private IRpcClient _rpcTransfer;
        private ConcurrentDictionary<Type, ClientServiceProxyInfo> _clientProxyDict = new ConcurrentDictionary<Type, ClientServiceProxyInfo>(); //The service proxy container for client.
        private ProxyGenerator _proxyGenerator = new ProxyGenerator(); //The service proxy generator for client.

        /// <summary>
        /// Gets or sets the RPC call timeout, in millseconds.The default timeout is 30000ms.
        /// </summary>
        public int RpcTimeout { get; set; } = 30000;

        /// <summary>
        /// Register the service proxy type to the channel.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        /// <param name="serviceCollectionName">The name of services that the TService belong to.</param>
        public void RegisterServiceProxy<TService>(string serviceCollectionName)
        {
            Type serviceType = typeof(TService);
            ClientServiceProxyInfo proxyInfo = new ClientServiceProxyInfo(serviceType, serviceCollectionName);
            if (_clientProxyDict.TryGetValue(serviceType, out ClientServiceProxyInfo oldInfo))
            {
                oldInfo.Dispose();
            }
            _clientProxyDict[serviceType] = proxyInfo;
        }

        /// <summary>
        /// UnRegister the service proxy type in the channel.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        public void UnRegisterServiceProxy<TService>()
        {
            Type serviceType = typeof(TService);
            _clientProxyDict.TryRemove(serviceType, out _);
        }

        /// <summary>
        /// Get the service proxy from the channel.The user can use the service proxy to call RPC service.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        /// <returns>The instance of the service proxy.</returns>
        public TService GetServiceProxy<TService>()
        {
            Type serviceType = typeof(TService);
            if (!_clientProxyDict.TryGetValue(serviceType, out ClientServiceProxyInfo proxyInfo))
            {
                throw new Exception("The service has not registered.");
            }
            if (proxyInfo.ServiceProxy == null)
            {
                proxyInfo.Interceptor = new RpcInterceptor(_rpcTransfer, RpcTimeout, proxyInfo.ServiceCollectionName);
                proxyInfo.ServiceProxy = _proxyGenerator.CreateInterfaceProxyWithoutTarget(proxyInfo.ServiceType, proxyInfo.Interceptor);
            }
            return (TService)proxyInfo.ServiceProxy;
        }

        /// <summary>
        /// Release all resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var info in _clientProxyDict.Values)
            {
                info.Dispose();
            }
            _clientProxyDict.Clear();
        }
    }
}
