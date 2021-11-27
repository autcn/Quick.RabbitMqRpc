using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quick.RabbitMq
{
    /// <summary>
    /// Provides functions to communicate with RabbitMq server.
    /// </summary>
    public class RabbitMqChannel : IDisposable
    {
        #region Constructor

        /// <summary>
        /// Create an instance of RabbitMqChannel class.
        /// </summary>
        /// <param name="connection">An instance of RabbitMqConnection class.</param>
        public RabbitMqChannel(RabbitMqConnection connection)
        {
            AttachAndOpen(connection);
        }

        /// <summary>
        /// Create an instance of RabbitMqChannel class. The constructor is only used in derived class.
        /// </summary>
        protected RabbitMqChannel()
        {
        }

        #endregion

        #region Private members

        /// <summary>
        /// The instance of MQ connection that used to transfer message.
        /// </summary>
        protected RabbitMqConnection _connection;

        /// <summary>
        /// The instance of channel that used for message sending and receiving.
        /// </summary>
        protected IModel _channel;

        private EventingBasicConsumer _consumer;
        private string _queueName;
        private HashSet<string> _routingKeySet { get; } = new HashSet<string>();

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the name of the exchange associated with the connection.
        /// </summary>
        public string Exchange => _connection.Exchange;

        /// <summary>
        /// Gets whether the channel is open or not.
        /// </summary>
        public bool IsOpen => _channel != null && _channel.IsOpen;

        #endregion

        #region Events

        /// <summary>
        /// The event will be triggered when MQ message received.
        /// </summary>
        public event EventHandler<BasicDeliverEventArgs> Received;

        /// <summary>
        /// The event will be triggered when the channel is shutdown.
        /// </summary>
        public event EventHandler<ShutdownEventArgs> ModelShutdown;

        /// <summary>
        /// The event will be triggered when an exception occurs.
        /// </summary>
        public event EventHandler<CallbackExceptionEventArgs> CallbackException;

        #endregion

        #region Private methods

        /// <summary>
        /// Attach the channel to existing connection, then open the channel.
        /// </summary>
        /// <param name="connection"></param>
        protected void AttachAndOpen(RabbitMqConnection connection)
        {
            _connection = connection;
            try
            {
                connection.AddChannel(this);
                Open();
            }
            catch
            {
                _connection = null;
                throw;
            }
        }

        private void EnsureConnected()
        {
            if (_channel == null || !_connection.IsOpen)
            {
                throw new Exception("The connection is disconnected.");
            }
        }

        private void _channel_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            CallbackException?.Invoke(this, e);
        }

        private void _channel_ModelShutdown(object sender, ShutdownEventArgs e)
        {
            ModelShutdown?.Invoke(this, e);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Open the channel.
        /// </summary>
        public virtual void Open()
        {
            if (IsOpen)
            {
                return;
            }

            if (_connection == null)
            {
                throw new Exception("Can not reopen a disposed channel.");
            }

            if (!_connection.IsOpen)
            {
                throw new Exception("The base connection is disconnected.");
            }

            //Create channel
            _channel = _connection.BaseConnection.CreateModel();
            _channel.ModelShutdown += _channel_ModelShutdown;
            _channel.CallbackException += _channel_CallbackException;

            //Create exchange
            if (!string.IsNullOrEmpty(Exchange))
            {
                _channel.ExchangeDeclare(exchange: Exchange,
                                             type: "topic",
                                          durable: false,
                                       autoDelete: true);
            }
            //If routing keys exist, subscribe.
            foreach (string routingKey in _routingKeySet.ToList())
            {
                string[] keys = routingKey.Split('|');
                SubscribeExchange(keys[0], keys[1]);
            }
        }

        /// <summary>
        /// Create an instance of IBasicProperties that can be sent to MQ.
        /// </summary>
        /// <returns></returns>
        public IBasicProperties CreateBasicProperties()
        {
            if (_channel == null)
            {
                return null;
            }
            return _channel.CreateBasicProperties();
        }

        /// <summary>
        /// Send text message to MQ.
        /// </summary>
        /// <param name="routingKey">The routing key of the message.</param>
        /// <param name="text">The text content of the message.</param>
        public void SendText(string routingKey, string text)
        {
            SendExchangeText(Exchange, routingKey, text);
        }

        /// <summary>
        /// Send text message to MQ.
        /// </summary>
        /// <param name="routingKey">The routing key of the message.</param>
        /// <param name="basicProperties">An instance of IBasicProperties which represents extra data of the message.</param>
        /// <param name="text">The text content of the message.</param>
        public void SendText(string routingKey, IBasicProperties basicProperties, string text)
        {
            SendExchangeText(Exchange, routingKey, basicProperties, text);
        }

        /// <summary>
        /// Send bytes message to MQ.
        /// </summary>
        /// <param name="routingKey">The routing key of the message.</param>
        /// <param name="data">The binary data that to be sent.</param>
        public void SendMessage(string routingKey, byte[] data)
        {
            SendExchangeMessage(Exchange, routingKey, data);
        }

        /// <summary>
        /// Send bytes message to MQ.
        /// </summary>
        /// <param name="routingKey">The routing key of the message.</param>
        /// <param name="basicProperties">An instance of IBasicProperties which represents extra data of the message.</param>
        /// <param name="body">The binary data that to be sent.</param>
        public void SendMessage(string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            SendBasicMessage(new BasicMessage()
            {
                Exchange = Exchange,
                RoutingKey = routingKey,
                BasicProperties = basicProperties,
                Body = body
            });
        }

        /// <summary>
        /// Send text message to specific exchange.
        /// </summary>
        /// <param name="exchange">The name of the exchange to which the message will be sent.</param>
        /// <param name="routingKey">The routing key of the message.</param>
        /// <param name="text">The text content of the message.</param>
        public void SendExchangeText(string exchange, string routingKey, string text)
        {
            var body = Encoding.UTF8.GetBytes(text);
            SendExchangeMessage(exchange, routingKey, body);
        }

        /// <summary>
        /// Send text message to specific exchange.
        /// </summary>
        /// <param name="exchange">The name of the exchange to which the message will be sent.</param>
        /// <param name="routingKey">The routing key of the message.</param>
        /// <param name="basicProperties">An instance of IBasicProperties which represents extra data of the message.</param>
        /// <param name="text">The text content of the message.</param>
        public void SendExchangeText(string exchange, string routingKey, IBasicProperties basicProperties, string text)
        {
            var body = Encoding.UTF8.GetBytes(text);
            SendBasicMessage(new BasicMessage()
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                BasicProperties = basicProperties,
                Body = body
            });
        }

        /// <summary>
        /// Send text message to specific exchange.
        /// </summary>
        /// <param name="exchange">The name of the exchange to which the message will be sent.</param>
        /// <param name="routingKey">The routing key of the message.</param>
        /// <param name="body">The body of the message, in bytes.</param>
        public void SendExchangeMessage(string exchange, string routingKey, byte[] body)
        {
            SendBasicMessage(new BasicMessage()
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                Body = body
            });
        }

        /// <summary>
        /// Send basic message to MQ.
        /// </summary>
        /// <param name="basicMessage">An instance of BasicMessage class.</param>
        public void SendBasicMessage(BasicMessage basicMessage)
        {
            EnsureConnected();
            _channel.BasicPublish(exchange: basicMessage.Exchange,
                                     routingKey: basicMessage.RoutingKey,
                                     basicProperties: basicMessage.BasicProperties,
                                     body: basicMessage.Body);
        }

        /// <summary>
        /// Subscribe routing keys in default exchange.
        /// </summary>
        /// <param name="routingKeys">The routing key collection.</param>
        public void Subscribe(params string[] routingKeys)
        {
            SubscribeExchange(Exchange, routingKeys);
        }

        /// <summary>
        /// Subscribe routing keys with specific exchange.
        /// </summary>
        /// <param name="exchange">The name of the exhange which the routing keys bind to.</param>
        /// <param name="routingKeys">The routing key collection.</param>
        public void SubscribeExchange(string exchange, params string[] routingKeys)
        {
            EnsureConnected();
            if (_queueName == null)
            {
                _queueName = _channel.QueueDeclare().QueueName;
            }

            foreach (var routingKey in routingKeys)
            {
                _channel.QueueBind(queue: _queueName,
                                  exchange: exchange,
                                  routingKey: routingKey);
                _routingKeySet.Add($"{exchange}|{routingKey}");
            }
            if (_consumer == null)
            {
                _consumer = new EventingBasicConsumer(_channel);
                _consumer.Received += (model, ea) =>
                {
                    Received?.Invoke(this, ea);
                };
                _channel.BasicConsume(_queueName, true, _consumer);
            }
        }

        /// <summary>
        /// UnSubscribe routing keys.
        /// </summary>
        /// <param name="routingKeys">The routing key collection.</param>
        public void UnSubscribe(params string[] routingKeys)
        {
            UnSubscribeExchange(Exchange, routingKeys);
        }

        /// <summary>
        /// UnSubscribe routing keys from specific exchange.
        /// </summary>
        /// <param name="exchange">The name of the exchange which the routing key had bound to.</param>
        /// <param name="routingKeys">The routing key collection.</param>
        public void UnSubscribeExchange(string exchange, params string[] routingKeys)
        {
            EnsureConnected();
            if (_queueName == null || _consumer == null)
            {
                return;
            }
            foreach (var routingKey in routingKeys)
            {
                _channel.QueueUnbind(queue: _queueName,
                                  exchange: exchange,
                                  routingKey: routingKey);
                _routingKeySet.Remove($"{exchange}|{routingKey}");
            }
        }

        /// <summary>
        /// UnSubscribe all routing keys from all exchanges.
        /// </summary>
        public void UnSubscribeAll()
        {
            EnsureConnected();
            foreach (var routingKey in _routingKeySet)
            {
                string[] keys = routingKey.Split('|');
                UnSubscribeExchange(keys[0], keys[1]);
            }
        }

        /// <summary>
        /// Close the channel but keep the subscribes.
        /// </summary>
        public void Close()
        {
            if (_channel != null)
            {
                _queueName = null;
                _channel.Close();
                _channel.Dispose();

                _channel = null;
                _consumer = null;
            }
        }

        /// <summary>
        /// Close the channel and release all resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (_channel != null)
            {
                _queueName = null;
                _channel.Close();
                _channel.Dispose();

                _connection.RemoveChannel(this);
                _connection = null;

                _channel = null;
                _consumer = null;

                _routingKeySet.Clear();
            }
        }

        #endregion
    }
}
