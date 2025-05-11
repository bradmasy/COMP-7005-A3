using BusinessLogic;
using static BusinessLogic.Constants;

namespace Client;

class Program
{
    static async Task Main(string[] args)
    {
        Client? client = null;

        try
        {
            Validator.ValidateClientArguments(args);

            var message = args[Message];
            var password = args[Password];
            var ipAddress = args[IpAddressIndex];
            var port = int.Parse(args[PortIndex]);

            client = new Client(ipAddress, port);

            await client.Connect();
            await client.Send(message, password);

            var data = await client.Receive();

            if (data.Contains("ERROR")) throw new Exception(data);

            var decrypted = EncryptionService.Decrypt(data, password);

            client.DisplayMessage(decrypted);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            client?.Teardown();
        }
    }
}