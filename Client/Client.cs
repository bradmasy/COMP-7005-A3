using System.Net;
using System.Net.Sockets;
using System.Text;
using BusinessLogic;
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

    public byte[] EncryptFile(string file, string password)
    {
        return EncryptionService.Encrypt(file, password);
    }
    public async Task Send(byte[] message)
    {
        var descriptor = await Socket.SendAsync(message, SocketFlags.None);

        if (descriptor <= NoDataSent) throw new Exception("Error sending data");
    }

    public async Task<MemoryStream> ReadAndCreateFileInMemory(string filePath,string password)
    {

        using var ms = new MemoryStream();
        using (FileStream file = new(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] bytes = new byte[file.Length];

            await file.ReadAsync(bytes.AsMemory(0, (int)file.Length));

            string fileContent = System.Text.Encoding.UTF8.GetString(bytes);
            var encryptedBytes = EncryptFile(fileContent,password);

            ms.Write(encryptedBytes, 0, (int)file.Length);
        }

        return ms;

    }
    public async Task<string> Receive()
    {
        var buffer = new byte[ByteArraySize];
        var numberOfBytesReceived = await Socket.ReceiveAsync(buffer, SocketFlags.None);

        if (numberOfBytesReceived <= NoBytes) return string.Empty;
        var receivedMessage = Encoding.UTF8.GetString(buffer, 0, numberOfBytesReceived);
        return receivedMessage;
    }

    public void Teardown()
    {
        Socket.Close();
    }

    public void DisplayMessage(string message)
    {
        Console.WriteLine($"The Decrypted Message is: \"{message}\"");
    }
}