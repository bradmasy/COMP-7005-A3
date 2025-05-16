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
            var beforeEncryptionDelaySeconds = int.Parse(args[ConfigurableDelayBefore]);
            var afterEncryptionDelaySeconds = int.Parse(args[ConfigurableDelayAfter]);

            var server = new Server(ipAddress, port, beforeEncryptionDelaySeconds, afterEncryptionDelaySeconds);

            await server.Run();

            server.TearDown();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}