using Newtonsoft.Json;

namespace Quick.Rpc
{
    internal static class RpcInvocationSerializerSettings
    {
        internal readonly static JsonSerializerSettings Default;
        static RpcInvocationSerializerSettings()
        {
            Default = new JsonSerializerSettings()
            {
                //TypeNameHandling = TypeNameHandling.Objects,
                //TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };
        }
    }
}
