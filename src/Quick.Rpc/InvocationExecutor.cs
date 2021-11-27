using System;
using System.Reflection;

namespace Quick.Rpc
{
    internal static class InvocationExecutor
    {
        internal static object Execute(object serviceInstance, InvocationData invData)
        {
            Type serviceType = serviceInstance.GetType();
            MethodInfo[] serviceMethods = serviceType.GetMethods();
            MethodInfo targetMethod = null;
            bool isMethodNameExist = false;
            foreach (MethodInfo serviceMethod in serviceMethods)
            {
                if (serviceMethod.Name == invData.MethodName)
                {
                    isMethodNameExist = true;
                    Type[] genericArgs = serviceMethod.GetGenericArguments();
                    if (genericArgs.Length != invData.GenericArguments.Length)
                    {
                        continue;
                    }

                    ParameterInfo[] serviceParamInfos = serviceMethod.GetParameters();
                    if (serviceParamInfos.Length != invData.ArgumentTypes.Length)
                    {
                        continue;
                    }

                    bool isParamSame = true;

                    if (serviceParamInfos.Length > 0)
                    {
                        for (int i = 0; i < serviceParamInfos.Length; i++)
                        {
                            if (serviceParamInfos[i].ParameterType != invData.ArgumentTypes[i])
                            {
                                isParamSame = false;
                                break;
                            }
                        }
                    }
                    if (isParamSame)
                    {
                        targetMethod = serviceMethod;
                        break;
                    }
                }
            }
            if (targetMethod == null)
            {
                if(isMethodNameExist)
                {
                    throw new RpcMethodNotMatchException();
                }
                else
                {
                    throw new RpcMethodNotFoundException();
                }
            }
            if (invData.GenericArguments.Length > 0)
            {
                targetMethod = targetMethod.MakeGenericMethod(invData.GenericArguments);
            }
            try
            {
                return targetMethod.Invoke(serviceInstance, invData.Arguments);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
