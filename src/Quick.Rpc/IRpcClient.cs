using System;

namespace Quick.Rpc
{
    /// <summary>
    /// The interface that provides functions to send and recv RPC data.
    /// </summary>
    public interface IRpcClient
    {
        /// <summary>
        /// Send serialized invocation data to server.
        /// </summary>
        /// <param name="context">The invocation context that will be used during sending.</param>
        void SendInvocation(SendInvocationContext context);

        /// <summary>
        /// The event will be triggered when RPC return data is received.
        /// </summary>
        event EventHandler<RpcReturnDataEventArgs> RpcReturnDataReceived;
    }

    /// <summary>
    /// The event arguments that contains the data responsed from RPC server.
    /// </summary>
    public class RpcReturnDataEventArgs : EventArgs
    {
        /// <summary>
        /// Create an instance of RpcReturnDataEventArgs class.
        /// </summary>
        /// <param name="data">The data responsed from RPC server.</param>
        public RpcReturnDataEventArgs(byte[] data)
        {
            Data = data;
        }

        /// <summary>
        /// Gets the data responsed from RPC server..
        /// </summary>
        public byte[] Data { get; }
    }

    /// <summary>
    /// Represents a invocation context while sending to server.
    /// </summary>
    public class SendInvocationContext
    {
        /// <summary>
        /// Create an instance of SendInvocationContext.
        /// </summary>
        /// <param name="id">The id of the invocation.</param>
        /// <param name="invocationBytes">The content of the invocation, in bytes.</param>
        /// <param name = "serviceToken" >The custom token associated with client registered service.</param>
        public SendInvocationContext(Guid id, byte[] invocationBytes, object serviceToken)
        {
            Id = id;
            InvocationBytes = invocationBytes;
            ServiceToken = serviceToken;
        }

        /// <summary>
        /// Create an instance of SendInvocationContext.
        /// </summary>
        /// <param name="id">The id of the invocation.</param>
        /// <param name="invocationBytes">The content of the invocation, in bytes.</param>
        public SendInvocationContext(Guid id, byte[] invocationBytes) : this(id, invocationBytes, null)
        {

        }

        /// <summary>
        /// Gets the id of invockation
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the content of invocation,in bytes.
        /// </summary>
        public byte[] InvocationBytes { get; }

        /// <summary>
        /// The custom token associated with client registered service.
        /// </summary>
        public object ServiceToken { get; }
    }
}
