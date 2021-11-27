# Quick.RabbitMqRpc
It contains a simple RPC framework and an implementation based on RabbitMQ.

The nuget url is:  https://www.nuget.org/packages/Quick.RabbitMq/1.0.0-beta.1

## Step1:  Protocol 

Create a dll project to define the RPC protocol. 

``` csharp
public interface IOrderService
{
    LoginResult Login(LoginRequest request);
}
public class LoginRequest
{
    public string User { get; set; }
    public string Password { get; set; }
}

public class LoginResult
{
    public int UserId { get; set; }
    public bool IsSuccess { get; set; }
    public string Remark { get; set; }
    public string Token { get; set; }
}
```

## Step2:  Server

2.1 Reference the protocol dll and then implements the service interfaces.

``` csharp
public class OrderService : IOrderService
{
    public LoginResult Login(LoginRequest request)
    {
        if (request == null)
        {
            throw new Exception("The request parameter is required.");
        }
        LoginResult result = new LoginResult();
        if (request.User == "admin" && request.Password == "password")
        {
            result.UserId = 2334143;
            result.IsSuccess = true;
            result.Token = "2938d828s8a8823";
        }
        else
        {
            result.Remark = "The user name or password is invalid.";
        }
        return result;
    }
}
```

2.2 Start the RPC service

``` c#
 class Program
 {
     static void Main(string[] args)
     {
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
```

## Step 3:  Client

Just reference the protocol dll created in step 1.

``` c#
 class Program
 {
     static void Main(string[] args)
     {
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
					
                     //Run the RPC test.
                     LoginRequest request = new LoginRequest()
                    {
                        User = "admin",
                        Password = "password"
                    };
                    LoginResult loginResult = orderService.Login(request);
                    Console.WriteLine(JsonConvert.SerializeObject(loginResult, Formatting.Indented));

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
 }
```

