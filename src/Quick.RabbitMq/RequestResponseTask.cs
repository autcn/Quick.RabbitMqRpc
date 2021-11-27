using System.Net;
using System.Threading;

namespace Quick.RabbitMq
{
    internal class RequestResponseTask
    {
        internal RequestResponseTask(string taskId)
        {
            WaitEvent = new ManualResetEvent(false);
            TaskId = taskId;
        }

        public string TaskId { get; }
        public object Result { get; set; }
        public HttpStatusCode StatusCode { get; set; } 
        public string ExceptionMessage { get; set; }
        public ManualResetEvent WaitEvent { get; }
    }
}
