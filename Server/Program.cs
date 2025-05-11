using BusinessLogic;
using static BusinessLogic.Constants;

namespace Server;
class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Validator.ValidateServerArgs(args);

            var ipAddress = args[IpAddress];
            var port = int.Parse(args[Port]);

            var server = new Server(ipAddress, port);

            await server.Run();

            server.TearDown();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}