using System;
using System.Net;
using System.Threading;

namespace Quick.Rpc
{
    internal class RequestResponseTask : IDisposable
    {
        public RequestResponseTask(Guid taskId, Type returnType)
        {
            WaitEvent = new ManualResetEvent(false);
            TaskId = taskId;
            ReturnType = returnType;
        }

        public Guid TaskId { get; }
        public object Result { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string ExceptionMessage { get; set; }
        public ManualResetEvent WaitEvent { get; }
        public Type ReturnType { get; }

        public void Dispose()
        {
            WaitEvent?.Close();
            WaitEvent?.Dispose();
        }
    }
}
