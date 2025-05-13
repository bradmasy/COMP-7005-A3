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

            var filePath = args[FilePath];
            var ipAddress = args[IpAddress];
            var port = int.Parse(args[Port]);
            var password = args[EncryptionPassword];

            client = new Client(ipAddress, port);

            await client.Connect();
            
            var fileMemoryStream = await client.ReadAndCreateFileInMemory(filePath, password);
            var bytes = fileMemoryStream.ToArray();
          
            await client.Send(bytes);

            var data = await client.Receive();

           // if (data.Contains("ERROR")) throw new Exception(data);

           // var decrypted = EncryptionService.Decrypt(data, password);

          //  client.DisplayMessage(decrypted);
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