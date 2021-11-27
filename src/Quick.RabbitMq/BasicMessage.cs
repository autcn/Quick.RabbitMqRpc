using RabbitMQ.Client;

namespace Quick.RabbitMq
{
    /// <summary>
    /// The basic message sent by channel.
    /// </summary>
    public class BasicMessage
    {
        /// <summary>
        /// Gets or sets the name of exchange. The default value is "";
        /// </summary>
        public string Exchange { get; set; } = "";

        /// <summary>
        /// Gets or sets the routing key of the message.
        /// </summary>
        public string RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets the message basic properties as extra data of the sending message.
        /// </summary>
        public IBasicProperties BasicProperties { get; set; }

        /// <summary>
        /// Gets or sets the body of the sending message, in bytes.
        /// </summary>
        public byte[] Body { get; set; }
    }
}
