using System;
using System.Net;

namespace Quick.Rpc
{
    /// <summary>
    /// The based class for RPC exception
    /// </summary>
    public class RpcException : Exception
    {
        /// <summary>
        /// Create an instance of RpcException class.
        /// </summary>
        /// <param name="code">The code of the exception.</param>
        /// <param name="message">The text message of the exception.</param>
        public RpcException(int code, string message) : base(message)
        {
            Code = code;
        }

        /// <summary>
        /// Create an instance of RpcException class with specific code.
        /// </summary>
        /// <param name="code"></param>
        public RpcException(int code) : this(code, null)
        {

        }

        /// <summary>
        /// Gets the code of the exception.
        /// </summary>
        public int Code { get; }
    }

    /// <summary>
    /// Represents an exception that thrown as the RPC call is timeout.
    /// </summary>
    public class RpcTimeoutException : RpcException
    {
        /// <summary>
        /// Create an instance of RpcTimeoutException class.
        /// </summary>
        public RpcTimeoutException() : base((int)HttpStatusCode.RequestTimeout, "Request timeout.")
        {
        }
    }

    internal class RpcMethodNotFoundException : RpcException
    {
        public RpcMethodNotFoundException() : base((int)HttpStatusCode.NotFound, "The method is not found.")
        {
        }
    }

    internal class RpcMethodNotMatchException : RpcException
    {
        public RpcMethodNotMatchException() : base((int)HttpStatusCode.NotFound, "The method parameters do not match.")
        {
        }
    }
}
