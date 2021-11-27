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
        /// <param name="invocationBytes">The invocation data sent to server, in bytes.</param>
        /// <param name="serviceToken">The custom token associated with client registered service.</param>
        void SendInvocation(byte[] invocationBytes, object serviceToken);

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
}
