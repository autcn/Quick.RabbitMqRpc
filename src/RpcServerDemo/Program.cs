using Quick.RabbitMq;
using RpcProtocolDemo;
using System;

namespace RpcServerDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            //The exchange name is used for business isolation. The topic is unique in one exchange, but can be same in different exchanges. 
            try
            {
                using (RabbitMqConnection connection = new RabbitMqConnection("TradeServer"))
                {
                    connection.Connect("amqp://tx.cvbox.cn:5672/", "test", "123456");
                    using (RabbitMqRpcChannel serverChannel = new RabbitMqRpcChannel(connection, "SRV_01"))
                    {
                        //Add service which used to execute RPC calls.
                        serverChannel.AddServerService<IOrderService>(new OrderService());

                        Console.WriteLine("The server is running! \r\nPress any key to exit...");
                        Console.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
    }
}
