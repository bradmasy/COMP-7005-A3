using System.Net;
using System.Net.Sockets;
using System.Text;
using BusinessLogic;
using Microsoft.VisualBasic;
using static BusinessLogic.Constants;

namespace Client;

public class Client(string ipAddress, int port)
{
    private Socket Socket { get; set; } = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    public async Task Connect()
    {
        var endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        await Socket.ConnectAsync(endPoint);
    }

    private string EncryptFileToString(string file, string password)
    {
        return EncryptionService.EncryptToString(file, password);
    }

    public async Task Send(byte[] message)
    {
        var descriptor = await Socket.SendAsync(message, SocketFlags.None);

        if (descriptor <= NoDataSent) throw new Exception("Error sending data");
    }

    public async Task<MemoryStream> ReadAndCreateFileInMemory(string filePath, string password)
    {
        var ms = new MemoryStream();

        await using FileStream file = new(filePath, FileMode.Open, FileAccess.Read);

        var bytes = new byte[file.Length];

        var read = await file.ReadAsync(bytes.AsMemory(0, (int)file.Length));

        // return the file as a string
        var fileContent = Encoding.ASCII.GetString(bytes);

        // encrypt just the file
        var encryptedFileStr = EncryptFileToString(fileContent, password);

        // attach the password not encrypted to decrypt file on server side
        var payload = $"{password}|{encryptedFileStr}";

        // convert payload to bytes
        var convertedToBytes = Encoding.ASCII.GetBytes(payload);

        ms.Write(convertedToBytes, 0, convertedToBytes.Length);

        return ms;
    }

    public async Task<string> Receive()
    {
        var availableBytes = NoBytes;

        // keep polling the socket until we know how many bytes are available
        while (availableBytes <= NoBytes)
        {
            availableBytes = Socket.Available;
        }

        var buffer = new byte[availableBytes];
        var numberOfBytesReceived = await Socket.ReceiveAsync(buffer, SocketFlags.None);

        if (numberOfBytesReceived <= NoBytes) return string.Empty;

        var receivedMessage = Encoding.ASCII.GetString(buffer, 0, numberOfBytesReceived);

        return receivedMessage;
    }

    public void Teardown()
    {
        Socket.Close();
    }

    public void DisplayMessage(string message)
    {
        Console.WriteLine($"The Decrypted Message is:\n{message}");
    }

    public void IsError(string data)
    {
        if (data.Contains(Error)) throw new Exception(data);
    }
}