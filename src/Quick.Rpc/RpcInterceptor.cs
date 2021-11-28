using Castle.DynamicProxy;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;

namespace Quick.Rpc
{
    internal class RpcInterceptor : IInterceptor, IDisposable
    {
        #region Constructor

        internal RpcInterceptor(IRpcClient rpcTransfer, int rpcTimeout, object serviceToken)
        {
            _serviceToken = serviceToken;
            _rpcTimeout = rpcTimeout;
            _rpcTransfer = rpcTransfer;
            _rpcTransfer.RpcReturnDataReceived += _rpcTransfer_RpcReturnDataReceived;
        }

        #endregion

        #region Private members

        private int _rpcTimeout;
        private IRpcClient _rpcTransfer;
        private object _serviceToken;
        private ConcurrentDictionary<Guid, RequestResponseTask> _requestTaskDict = new ConcurrentDictionary<Guid, RequestResponseTask>(); //The RPC task container.

        #endregion


        #region Private methods

        private void _rpcTransfer_RpcReturnDataReceived(object sender, RpcReturnDataEventArgs e)
        {
            if (e.Data == null || e.Data.Length == 0)
            {
                return;
            }

            string returnJson = Encoding.UTF8.GetString(e.Data);
            ReturnData returnData = null;
            try
            {
                returnData = JsonConvert.DeserializeObject<ReturnData>(returnJson, RpcInvocationSerializerSettings.Default);
                if (returnData.Id == Guid.Empty)
                {
                    return;
                }
            }
            catch
            {
                return;
            }

            Guid taskId = returnData.Id;

            //The task id does not exists. It means the request is time out, or discarded.
            if (!_requestTaskDict.TryGetValue(taskId, out RequestResponseTask task))
            {
                return;
            }

            Type returnDataType = null;
            if (returnData.HttpStatusCode == (int)HttpStatusCode.OK)
            {
                returnDataType = typeof(ReturnData<>).MakeGenericType(task.ReturnType == typeof(void) ? typeof(object) : task.ReturnType);
                returnData = (ReturnData)JsonConvert.DeserializeObject(returnJson, returnDataType, RpcInvocationSerializerSettings.Default);
            }

            try
            {
                //Extract status firstly.
                HttpStatusCode statusCode = (HttpStatusCode)returnData.HttpStatusCode;

                task.StatusCode = statusCode;
                if (statusCode == HttpStatusCode.InternalServerError
                  || statusCode == HttpStatusCode.BadRequest
                  || statusCode == HttpStatusCode.NotFound)
                {
                    task.ExceptionMessage = returnData.ExceptionMessage;
                }
                else
                {
                    if (statusCode == HttpStatusCode.OK)
                    {
                        var returnObjectProp = returnDataType.GetProperty(nameof(ReturnData<int>.ReturnObject));
                        task.Result = returnObjectProp.GetValue(returnData);
                    }
                    else
                    {
                        task.ExceptionMessage = statusCode.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                task.ExceptionMessage = ex.Message;
            }

            task.WaitEvent.Set();
        }

        private InvocationData Convert(IInvocation invocation)
        {
            InvocationData data = new InvocationData();
            data.Id = Guid.NewGuid();
            data.ReturnType = invocation.Method.ReturnType;
            data.MethodDeclaringType = invocation.Method.DeclaringType;
            data.MethodName = invocation.Method.Name;
            data.GenericArguments = invocation.GenericArguments ?? new Type[0];
            data.Arguments = invocation.Arguments;
            data.ArgumentTypes = invocation.Method.GetParameters().Select(p => p.ParameterType).ToArray();
            return data;
        }

        #endregion

        #region Public methods

        public void Intercept(IInvocation invocation)
        {
            //Serialize
            InvocationData data = Convert(invocation);
            Guid taskId = data.Id;
            string invocationJson = JsonConvert.SerializeObject(data, RpcInvocationSerializerSettings.Default);

            RequestResponseTask task = new RequestResponseTask(taskId, invocation.Method.ReturnType);
            _requestTaskDict[taskId] = task;

            //Send the invocation data
            byte[] invocationBytes = Encoding.UTF8.GetBytes(invocationJson);
            _rpcTransfer.SendInvocation(new SendInvocationContext(taskId, invocationBytes, _serviceToken));

            if (!task.WaitEvent.WaitOne(_rpcTimeout))
            {
                //Remove the task if time out.
                _requestTaskDict.TryRemove(taskId, out _);
                task.Dispose();
                throw new RpcTimeoutException();
            }

            _requestTaskDict.TryRemove(taskId, out _);

            if (task.StatusCode != HttpStatusCode.OK)
            {
                throw new RpcException((int)task.StatusCode, task.ExceptionMessage);
            }

            invocation.ReturnValue = task.Result;
            task.Dispose();
        }

        public void Dispose()
        {
            _rpcTransfer.RpcReturnDataReceived -= _rpcTransfer_RpcReturnDataReceived;
        }

        #endregion  
    }
}
