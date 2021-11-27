﻿using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;

namespace Quick.RabbitMq
{
    /// <summary>
    /// Provides functions to manage connection to MQ server.
    /// </summary>
    public class RabbitMqConnection : IDisposable
    {
        #region Constructor

        /// <summary>
        /// Create an instancet of RabbitMqConnection
        /// </summary>
        /// <param name="exchange">The name of the exchange associated with the connection.</param>
        public RabbitMqConnection(string exchange)
        {
            if (string.IsNullOrEmpty(exchange))
            {
                throw new Exception("The exchange is required.");
            }
            Exchange = exchange;
        }

        #endregion

        #region Private members

        private IConnectionFactory _factory;
        private IConnection _connection;
        private object _lockObj = new object();
        private List<RabbitMqChannel> _channels = new List<RabbitMqChannel>();

        #endregion

        #region Public properties

        /// <summary>
        /// The name of the exchange.
        /// </summary>
        public string Exchange { get; }

        /// <summary>
        /// The uri string used by connection.
        /// </summary>
        public string Uri { get; private set; }

        /// <summary>
        /// The name the user that registered on MQ server.
        /// </summary>
        public string User { get; private set; }

        /// <summary>
        /// The password of the registered user on MQ server.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Gets whether the connection is open;
        /// </summary>
        public bool IsOpen => _connection != null && _connection.IsOpen;

        /// <summary>
        /// Gets the channels associated with the connection.
        /// </summary>
        public IReadOnlyList<RabbitMqChannel> Channels => _channels;

        internal IConnection BaseConnection => _connection;

        #endregion

        #region Events

        /// <summary>
        /// The event will be triggered when the connection is shutdown.
        /// </summary>
        public event EventHandler<ShutdownEventArgs> ConnectionShutdown;

        /// <summary>
        /// The event will be triggered when an exception occurs.
        /// </summary>
        public event EventHandler<CallbackExceptionEventArgs> CallbackException;

        /// <summary>
        /// The event will be triggered when the connection blocked.
        /// </summary>
        public event EventHandler<ConnectionBlockedEventArgs> ConnectionBlocked;

        /// <summary>
        /// The event will be triggered when the connection unblocked.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionUnblocked;

        #endregion

        #region Private methods

        private void EnsureConnected()
        {
            if (!IsOpen)
            {
                throw new Exception("The connection is disconnected.");
            }
        }

        private void _connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            ConnectionShutdown?.Invoke(this, e);
        }

        private void _connection_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            CallbackException?.Invoke(this, e);
        }

        private void _connection_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            ConnectionBlocked?.Invoke(this, e);
        }

        private void _connection_ConnectionUnblocked(object sender, EventArgs e)
        {
            ConnectionUnblocked?.Invoke(this, e);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Connect to the MQ server with an instance of IConnectionFactory which provides full settings.
        /// </summary>
        /// <param name="connectionFactory"></param>
        public void Connect(IConnectionFactory connectionFactory)
        {
            _factory = connectionFactory;
            _connection = _factory.CreateConnection();
            _connection.CallbackException += _connection_CallbackException;
            _connection.ConnectionShutdown += _connection_ConnectionShutdown;
            _connection.ConnectionUnblocked += _connection_ConnectionUnblocked;
            _connection.ConnectionBlocked += _connection_ConnectionBlocked;
            if (_channels != null)
            {
                foreach (var channel in _channels)
                {
                    channel.Open();
                }
            }
        }

        /// <summary>
        /// Connect to the MQ server with most popular parameters.
        /// </summary>
        /// <param name="uri">The uri string used by connection.</param>
        /// <param name="user">The name the user that registered on MQ server.</param>
        /// <param name="password">The password of the registered user on MQ server.</param>
        public void Connect(string uri, string user, string password)
        {
            Uri = uri;
            User = user;
            Password = password;
            if (_connection != null && _connection.IsOpen)
            {
                return;
            }
            _factory = new ConnectionFactory
            {
                UserName = user,
                Password = password,
                Endpoint = new AmqpTcpEndpoint(new Uri(uri)),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(4)
            };
            Connect(_factory);
        }

        /// <summary>
        /// Create a channel with current connection.
        /// </summary>
        /// <returns>An instance of channel.</returns>
        public RabbitMqChannel CreateChannel()
        {
            return new RabbitMqChannel(this);
        }

        internal void AddChannel(RabbitMqChannel channel)
        {
            EnsureConnected();
            lock (_lockObj)
            {
                _channels.Add(channel);
            }
        }

        internal void RemoveChannel(RabbitMqChannel channel)
        {
            lock (_lockObj)
            {
                _channels.Remove(channel);
            }
        }

        /// <summary>
        /// Close all the channels and current connection but not release resources. The user can open it again.
        /// </summary>
        public void Close()
        {
            if (_connection != null)
            {
                lock (_lockObj)
                {
                    foreach (RabbitMqChannel channel in _channels)
                    {
                        channel.Close();
                    }
                }

                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        /// <summary>
        /// Close channels and connection, then release all the resources.
        /// </summary>
        public void Dispose()
        {
            if (_connection != null)
            {
                lock (_lockObj)
                {
                    foreach (RabbitMqChannel channel in _channels)
                    {
                        channel.Close();
                    }
                }

                _channels.Clear();
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        #endregion
    }
}
