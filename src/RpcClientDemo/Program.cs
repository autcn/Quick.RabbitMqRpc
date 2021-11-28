using Newtonsoft.Json;
using Quick.RabbitMq;
using RpcProtocolDemo;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quick.RabbitMqRpc
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
                    using (RabbitMqRpcChannel clientChannel = new RabbitMqRpcChannel(connection))
                    {

                        //Register service proxy type for RPC calls.
                        clientChannel.RegisterClientServiceProxy<IOrderService>("SRV_01");

                        //Get service proxy interface
                        IOrderService orderService = clientChannel.GetClientServiceProxy<IOrderService>();

                        TestCalls(orderService);
                        TestMultiThread(orderService);

                        Console.WriteLine("RPC calls ended! \r\nPress any key to exit...");
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

        static void TestMultiThread(IOrderService orderService)
        {
            for (int i = 1; i <= 3; i++)
            {
                Task.Factory.StartNew(idx =>
                {
                    for (int j = 1; j <= 100; j++)
                    {
                        DateTime dateTime = orderService.GetServerTime();
                        Console.WriteLine($"{idx}_{j}: {dateTime}");
                    }
                }, i);
            }
        }

        static void TestCalls(IOrderService orderService)
        {
            //1
            bool isUserExist = orderService.IsUserNameExist("admin");
            Console.WriteLine(isUserExist ? "The user \"admin\" is exists." : "The user \"admin\" is not exists.");

            //2
            LoginRequest request = new LoginRequest()
            {
                User = "admin",
                Password = "password"
            };
            LoginResult loginResult = orderService.Login(request);
            Console.WriteLine(JsonConvert.SerializeObject(loginResult, Formatting.Indented));

            //3
            try
            {
                List<Order> orderList = orderService.GetUserOrderList(loginResult.UserId, loginResult.Token);
                Console.WriteLine(JsonConvert.SerializeObject(orderList, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //4
            double amount = orderService.SubmitOrder(123456);
            Console.WriteLine($"Order submitted successfully. The total amount is {amount}");

            //5
            orderService.LockUser(123456, null);
            orderService.LockUser(123456, 4);

            //6
            int[] users = new int[2] { 1, 2 };
            foreach (var id in users)
            {
                int? days = orderService.GetUserLockDays(id);
                if (days == null)
                {
                    Console.WriteLine("The user is not locked.");
                }
                else
                {
                    Console.WriteLine($"The user locked for {days.Value} days.");
                }
            }

            //7
            orderService.Ping();
            Console.WriteLine("Ping server successfully.");

            //8
            DateTime serverTime = orderService.GetServerTime();
            Console.WriteLine($"The server time is {serverTime}");
        }
    }
}
