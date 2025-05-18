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
        // Send the length as a long (8 bytes)
        var lengthBytes = BitConverter.GetBytes((long)message.Length);

        await Socket.SendAsync(lengthBytes, SocketFlags.None);

        // Send the actual data
        var descriptor = await Socket.SendAsync(message, SocketFlags.None);
        if (descriptor <= NoDataSent) throw new Exception("Error sending data");
    }

    public async Task<MemoryStream> ReadAndCreateFileInMemory(string filePath, string password)
    {
        var ms = new MemoryStream();

        await using FileStream file = new(filePath, FileMode.Open, FileAccess.Read);

        var bytes = new byte[file.Length];

        var read = await file.ReadAsync(bytes.AsMemory(0, (int)file.Length));
        var fileContent = Encoding.ASCII.GetString(bytes);
        var encryptedFileStr = EncryptFileToString(fileContent, password);
        var payload = $"{password}|{encryptedFileStr}";
        var convertedToBytes = Encoding.ASCII.GetBytes(payload);

        ms.Write(convertedToBytes, 0, convertedToBytes.Length);

        return ms;
    }

    public async Task<string> Receive()
    {
        var chunks = new List<byte[]>();
        var totalBytesReceived = 0;
        var buffer = new byte[Kb];

        while (true)
        {
            var bytesReceived = await Socket.ReceiveAsync(buffer, SocketFlags.None);

            if (bytesReceived == NoBytes) break;

            var chunk = new byte[bytesReceived];

            Array.Copy(buffer, chunk, bytesReceived);

            chunks.Add(chunk);
            totalBytesReceived += bytesReceived;
        }

        if (totalBytesReceived == NoBytes) return string.Empty;

        var completeData = new byte[totalBytesReceived];
        var offset = 0;

        foreach (var chunk in chunks)
        {
            Array.Copy(chunk, 0, completeData, offset, chunk.Length);
            offset += chunk.Length;
        }

        return Encoding.ASCII.GetString(completeData, 0, totalBytesReceived);
    }


    public void Teardown()
    {
        Socket.Close();
    }

    public void DisplayMessage(string message)
    {
        Console.WriteLine($"The Decrypted Message is:\n\n{message}");
    }

    public void IsError(string data)
    {
        if (data.Contains(Error)) throw new Exception(data);
    }
}