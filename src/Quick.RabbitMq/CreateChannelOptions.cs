using System;
using System.Collections.Generic;
using System.Text;

namespace Quick.RabbitMq
{
    /// <summary>
    /// Represents the options for channel creation.
    /// </summary>
    public class CreateChannelOptions
    {
        /// <summary>
        /// Gets or sets if using auto ack after a message received.
        /// </summary>
        public bool AutoAck { get; set; } = true;

        /// <summary>
        /// Gets or sets the queue name, if empty, an auto name will be used.
        /// </summary>
        public string QueueName { get; set; } = "";

        /// <summary>
        /// Gets or sets if this queue will survive a broker restart?
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        /// Should this queue use be limited to its declaring connection? Such a queue will be deleted when its declaring connection closes.
        /// </summary>
        public bool Exclusive { get; set; }

        /// <summary>
        /// Should this queue be auto-deleted when its last consumer (if any) unsubscribes?
        /// </summary>
        public bool AutoDelete { get; set; } = true;

        /// <summary>
        /// Gets or sets the prefetch count.
        /// </summary>
        public ushort QosCount { get; set; }

        /// <summary>
        /// Gets or sets the prefetch size.
        /// </summary>
        public uint QosSize { get; set; }

        /// <summary>
        /// Gets or sets if the qos settings used for global.
        /// </summary>
        public bool IsGlobalQos { get; set; }

        /// <summary>
        /// Optional; additional queue arguments, e.g. "x-queue-type"
        /// </summary>
        public Dictionary<string, object> Arguments { get; set; }
    }
}
