using Quick.Rpc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace Quick.RabbitMq
{
    /// <summary>
    /// The RabbitMqRpcChannel class that provides functions for both RPC server and client.
    /// </summary>
    public class RabbitMqRpcChannel : RabbitMqChannel, IRpcClient
    {
        #region Constructor

        /// <summary>
        /// Create an instance of RabbitMqRpcChannel class.It provides functions as RPC server or client according to "isRpcClient" parameter .
        /// </summary>
        /// <param name="connection">An instance of RabbitMqConnection.</param>
        /// <param name="serviceCollectionName">The name of services that provided by RPC server.</param>
        /// <param name="isRpcClient">If true, it has the functions of both server and client, otherwise it is just the server.</param>
        public RabbitMqRpcChannel(RabbitMqConnection connection, string serviceCollectionName, bool isRpcClient)
        {
            if (string.IsNullOrWhiteSpace(serviceCollectionName))
            {
                throw new Exception("The serviceCollectionName is required.");
            }
            IsRpcServer = true;
            IsRpcClient = isRpcClient;
            _rpcServerExecutor = new RpcServerExecutor();
            _rpcRequestQueueName = RabbitMqRpcProperties.RpcRequestQueueNamePrefix + serviceCollectionName;
            _proxyGenerator = new ServiceProxyGenerator(this);
            AttachAndOpen(connection);
        }

        /// <summary>
        /// Create an instance of RabbitMqRpcChannel class.It provides functions as RPC server.
        /// </summary>
        /// <param name="connection">An instance of RabbitMqConnection.</param>
        /// <param name="serviceCollectionName">The name of services that provided by RPC server.</param>
        public RabbitMqRpcChannel(RabbitMqConnection connection, string serviceCollectionName) : this(connection, serviceCollectionName, false)
        {
        }

        /// <summary>
        /// Create an instance of RabbitMqRpcChannel class.It provides functions as RPC client.
        /// </summary>
        /// <param name="connection"></param>
        public RabbitMqRpcChannel(RabbitMqConnection connection)
        {
            IsRpcClient = true;
            _proxyGenerator = new ServiceProxyGenerator(this);
            AttachAndOpen(connection);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets if the channel is RPC server.
        /// </summary>
        public bool IsRpcServer { get; private set; }

        /// <summary>
        /// Gets if the channel is RPC client.
        /// </summary>
        public bool IsRpcClient { get; private set; }

        /// <summary>
        /// Gets or sets the request timeout, in millseconds.The default timeout is 30000ms.
        /// </summary>
        public int RpcTimeout
        {
            get => _proxyGenerator.RpcTimeout;
            set => _proxyGenerator.RpcTimeout = value;
        }

        #endregion

        #region Private members

        //Service
        private RpcServerExecutor _rpcServerExecutor; //The service executor for RPC server.
        private string _rpcRequestQueueName;  //The name of the queue created on server to receive the RPC request.
        private EventingBasicConsumer _rpcServerConsumer; //The consumer for RPC server to process RPC request.

        //Client
        private ServiceProxyGenerator _proxyGenerator; //The service proxy generator for client.
        private string _rpcClientQueueName; //The name of the queue created on client to receive the RPC replied result.
        private EventingBasicConsumer _rpcClientConsumer; //The consumer for RPC client to process RPC replied result.

        #endregion

        #region Events

        /// <summary>
        /// The event will be triggered when RPC return data is received.
        /// </summary>
        public event EventHandler<RpcReturnDataEventArgs> RpcReturnDataReceived;

        #endregion

        #region Public functions

        /// <summary>
        /// Open the channel.It is automatically called when created. It is only used to reopen after closing.
        /// </summary>
        public override void Open()
        {
            if (IsOpen)
            {
                return;
            }

            base.Open();

            if (IsRpcServer)
            {
                //Create a named request receive queue for RPC server
                _channel.QueueDeclare(queue: _rpcRequestQueueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: true,
                                     arguments: null);

                //Create consumer for RPC server to process the RPC request.
                _rpcServerConsumer = new EventingBasicConsumer(_channel);
                _rpcServerConsumer.Received += _rpcServerConsumer_Received;
                _channel.BasicConsume(_rpcRequestQueueName, true, _rpcServerConsumer);
            }

            if (IsRpcClient)
            {
                //Create a anonymous queue for RPC client.
                var clientQueueResult = _channel.QueueDeclare();
                _rpcClientQueueName = clientQueueResult.QueueName;

                //Create consumer for RPC client to process the replied result from RPC server.
                _rpcClientConsumer = new EventingBasicConsumer(_channel);
                _rpcClientConsumer.Received += _rpcClientConsumer_Received;
                _channel.BasicConsume(_rpcClientQueueName, true, _rpcClientConsumer);
            }
        }

        #endregion

        #region Server functions

        private void _rpcServerConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var body = e.Body.ToArray();
            var props = e.BasicProperties;

            //Invalid data，do not reply
            if (string.IsNullOrWhiteSpace(props.ReplyTo))
            {
                return;
            }
            try
            {
                _rpcServerExecutor.ExecuteAsync(body).ContinueWith(task =>
                {
                    BasicMessage basicMessage = new BasicMessage()
                    {
                        RoutingKey = props.ReplyTo,
                        Body = task.Result
                    };
                    SendBasicMessage(basicMessage);
                });
            }
            catch
            {

            }
        }

        /// <summary>
        /// Add service to RPC server container.
        /// </summary>
        /// <typeparam name="TInterface">The interface of service.</typeparam>
        /// <param name="instance">The instance of the service that implement the TInterface.</param>
        public void AddServerService<TInterface>(TInterface instance)
        {
            if (!IsRpcServer)
            {
                throw new Exception("The channel is not created as rpc server, please use other constructor instead.");
            }
            _rpcServerExecutor.AddService<TInterface>(instance);
        }

        /// <summary>
        /// Add service to RPC server container.
        /// </summary>
        /// <typeparam name="TInterface">The interface of service.</typeparam>
        /// <param name="constructor">The func delegate used to create service instance.</param>
        public void AddServerService<TInterface>(Func<TInterface> constructor)
        {
            if (!IsRpcServer)
            {
                throw new Exception("The channel is not created as rpc server, please use other constructor instead.");
            }
            _rpcServerExecutor.AddService<TInterface>(constructor);
        }

        #endregion

        #region Client functions

        private void _rpcClientConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            RpcReturnDataReceived?.Invoke(this, new RpcReturnDataEventArgs(e.Body.ToArray()));
        }

        /// <summary>
        /// Register the service proxy type to the channel.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        /// <param name="serviceCollectionName">The name of services that the TService belong to.</param>
        public void RegisterClientServiceProxy<TService>(string serviceCollectionName)
        {
            if (!IsRpcClient)
            {
                throw new Exception("The channel is not created as rpc client, please use other constructor instead.");
            }
            _proxyGenerator.RegisterServiceProxy<TService>(serviceCollectionName);
        }

        /// <summary>
        /// UnRegister the service proxy type in the channel.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        public void UnRegisterClientServiceProxy<TService>()
        {
            if (!IsRpcClient)
            {
                throw new Exception("The channel is not created as rpc client, please use other constructor instead.");
            }
            _proxyGenerator.UnRegisterServiceProxy<TService>();
        }

        /// <summary>
        /// Get the service proxy from the channel.The user can use the service proxy to call RPC service.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        /// <returns>The instance of the service proxy.</returns>
        public TService GetClientServiceProxy<TService>()
        {
            return _proxyGenerator.GetServiceProxy<TService>();
        }

        void IRpcClient.SendInvocation(byte[] invocationBytes, object serviceCustomData)
        {
            if (!IsRpcClient)
            {

            }
            string serviceCollectionName = serviceCustomData.ToString();
            string serverQueueName = RabbitMqRpcProperties.RpcRequestQueueNamePrefix + serviceCollectionName;
            IBasicProperties props = _channel.CreateBasicProperties();
            props.ReplyTo = _rpcClientQueueName;

            _channel.BasicPublish(
                exchange: "",
                routingKey: serverQueueName,
                basicProperties: props,
                body: invocationBytes);
        }

        /// <summary>
        /// Release all the resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            _proxyGenerator?.Dispose();
        }

        #endregion
    }
}
