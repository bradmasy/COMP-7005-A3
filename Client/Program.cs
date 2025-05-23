﻿using BusinessLogic;
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
            var payload = fileMemoryStream.ToArray();

            await client.Send(payload);

            var data = await client.Receive();

            client.IsError(data);

            client.DisplayMessage(data);
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