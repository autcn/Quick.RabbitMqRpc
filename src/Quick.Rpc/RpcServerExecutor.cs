using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Quick.Rpc
{
    /// <summary>
    /// The RPC service container that provides method to execute RPC calls.
    /// </summary>
    public class RpcServerExecutor : IDisposable
    {
        private enum ServiceRegisterType
        {
            Instance,
            Func
        }
        private class ServiceRegisterInfo
        {
            public ServiceRegisterInfo(object service, ServiceRegisterType type)
            {
                Service = service;
                Type = type;
            }

            public object Service { get; }
            public ServiceRegisterType Type { get; }
        }

        /// <summary>
        /// Gets or sets a service provider to create RPC call services.
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; }

        private ConcurrentDictionary<Type, ServiceRegisterInfo> _serviceDict = new ConcurrentDictionary<Type, ServiceRegisterInfo>(); //The service container for RPC server.

        private void DeserializeInvocationArgments(InvocationData invocationData, string requestJson)
        {
            List<int> diffList = new List<int>();
            for (int i = 0; i < invocationData.Arguments.Length; i++)
            {
                if (invocationData.Arguments[i] != null)
                {
                    if (invocationData.Arguments[i].GetType() != invocationData.ArgumentTypes[i])
                    {
                        if (invocationData.Arguments[i].GetType().IsSubclassOf(typeof(JToken)))
                        {
                            JToken token = (JToken)invocationData.Arguments[i];
                            invocationData.Arguments[i] = token.ToObject(invocationData.ArgumentTypes[i]);
                        }
                        else
                        {
                            diffList.Add(i);
                        }
                    }
                }
            }
            if (diffList.Count == 0)
            {
                return;
            }
            JObject jObject = JObject.Parse(requestJson);
            JArray jArray = (JArray)jObject[nameof(InvocationData.Arguments)];
            foreach (int index in diffList)
            {
                invocationData.Arguments[index] = jArray[index].ToObject(invocationData.ArgumentTypes[index]);
            }
        }

        /// <summary>
        /// Add service to RPC server container.
        /// </summary>
        /// <typeparam name="TInterface">The interface of service.</typeparam>
        /// <param name="instance">The instance of the service that implement the TInterface.</param>
        public void AddService<TInterface>(TInterface instance)
        {
            Type serviceType = typeof(TInterface);
            _serviceDict[serviceType] = new ServiceRegisterInfo(instance, ServiceRegisterType.Instance);
        }

        /// <summary>
        /// Add service to RPC server container.
        /// </summary>
        /// <typeparam name="TInterface">The interface of service.</typeparam>
        /// <param name="constructor">The func delegate used to create service instance.</param>
        public void AddService<TInterface>(Func<TInterface> constructor)
        {
            Type serviceType = typeof(TInterface);
            _serviceDict[serviceType] = new ServiceRegisterInfo(constructor, ServiceRegisterType.Func);
        }

        /// <summary>
        /// When a transport layer error occurs, the user can call this method to directly create an error message.
        /// </summary>
        /// <param name="id">The id of the invocation.</param>
        /// <param name="statusCode">The status code in http codes.</param>
        /// <param name="message">A message describing the error.</param>
        /// <returns></returns>
        public static byte[] CreateErrorResponse(Guid id, HttpStatusCode statusCode, string message)
        {
            ReturnData<object> returnData = new ReturnData<object>();
            returnData.Id = id;
            returnData.HttpStatusCode = (int)statusCode;
            returnData.ExceptionMessage = message;
            string responseJson = JsonConvert.SerializeObject(returnData, RpcInvocationSerializerSettings.Default);
            return Encoding.UTF8.GetBytes(responseJson);
        }

        /// <summary>
        /// Execute an RPC call with serialized data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<byte[]> ExecuteAsync(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return Task.FromResult((byte[])null);
            }
            return Task.Run(() =>
            {
                ReturnData returnData = new ReturnData<object>();
                do
                {
                    //Extract invocation
                    string requestJson = Encoding.UTF8.GetString(data);
                    InvocationData invocationData = null;
                    try
                    {
                        invocationData = JsonConvert.DeserializeObject<InvocationData>(requestJson, RpcInvocationSerializerSettings.Default);
                    }
                    catch
                    {
                        returnData.HttpStatusCode = (int)HttpStatusCode.BadRequest;
                        returnData.ExceptionMessage = HttpStatusCode.BadRequest.ToString();
                        break;
                    }

                    //Create return data object.
                    Type realType = typeof(ReturnData<>).MakeGenericType(invocationData.ReturnType == typeof(void) ? typeof(object) : invocationData.ReturnType);
                    returnData = (ReturnData)Activator.CreateInstance(realType);
                    returnData.Id = invocationData.Id;
                    returnData.HttpStatusCode = (int)HttpStatusCode.OK;

                    if (invocationData.ArgumentTypes.Length > 0)
                    {
                        //Due to the argument is in type of object, the default converts may be incorrect.
                        //So we need to convert them again through the "ArgumentTypes" parameter.
                        DeserializeInvocationArgments(invocationData, requestJson);
                    }
                    object service = null;
                    //Find service
                    if (!_serviceDict.TryGetValue(invocationData.MethodDeclaringType, out ServiceRegisterInfo serviceRegisterInfo))
                    {
                        if (ServiceProvider != null)
                        {
                            try
                            {
                                service = ServiceProvider.GetService(invocationData.MethodDeclaringType);
                            }
                            catch
                            {

                            }
                        }

                        if (service == null)
                        {
                            //The service is not found, reply 404
                            returnData.HttpStatusCode = (int)HttpStatusCode.NotFound;
                            returnData.ExceptionMessage = "The service is not found.";
                            break;
                        }
                    }

                    //Call service
                    try
                    {
                        if (service == null)
                        {
                            if (serviceRegisterInfo.Type == ServiceRegisterType.Instance)
                            {
                                service = serviceRegisterInfo.Service;
                            }
                            else if (serviceRegisterInfo.Type == ServiceRegisterType.Func)
                            {
                                var funcType = serviceRegisterInfo.Service.GetType();
                                var method = funcType.GetMethod("Invoke");
                                service = method.Invoke(serviceRegisterInfo.Service, null);
                            }
                        }

                        object returnObject = InvocationExecutor.Execute(service, invocationData);
                        var returnProperty = realType.GetProperty(nameof(ReturnData<int>.ReturnObject));
                        returnProperty.SetValue(returnData, returnObject);
                    }
                    catch (ArgumentException argsEx)
                    {
                        returnData.HttpStatusCode = (int)HttpStatusCode.BadRequest;
                        returnData.ExceptionMessage = argsEx.Message;
                        break;
                    }
                    catch (InvalidOperationException invalidEx)
                    {
                        returnData.HttpStatusCode = (int)HttpStatusCode.BadRequest;
                        returnData.ExceptionMessage = invalidEx.Message;
                        break;
                    }
                    catch (RpcException rpcException)
                    {
                        returnData.HttpStatusCode = rpcException.Code;
                        returnData.ExceptionMessage = rpcException.Message;
                        break;
                    }
                    catch (Exception ex)
                    {
                        //Internal server error, reply 500
                        returnData.HttpStatusCode = (int)HttpStatusCode.InternalServerError;
                        returnData.ExceptionMessage = ex.Message;
                        break;
                    }

                } while (false);

                //Serialize return value
                string responseJson = JsonConvert.SerializeObject(returnData, RpcInvocationSerializerSettings.Default);
                return Encoding.UTF8.GetBytes(responseJson);
            });
        }

        /// <summary>
        /// Release all resources.
        /// </summary>
        public void Dispose()
        {
            _serviceDict.Clear();
        }
    }
}
