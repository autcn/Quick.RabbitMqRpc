namespace Quick.RabbitMq
{
    /// <summary>
    /// Represents a static class that used to define constant properties for MQ rpc.
    /// </summary>
    public static class RabbitMqRpcProperties
    {
        /// <summary>
        /// The name prefix of the RPC server queue which received RPC call request.
        /// </summary>
        public const string RpcRequestQueueNamePrefix = "quick.rpc.";
    }
}
