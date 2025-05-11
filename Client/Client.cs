using System.Net;
using System.Net.Sockets;
using System.Text;
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

    public async Task Send(string message, string password)
    {
        var bytes = Encoding.ASCII.GetBytes($"{message}|{password}");
        var descriptor = await Socket.SendAsync(bytes, SocketFlags.None);

        if (descriptor <= NoDataSent) throw new Exception("Error sending data");
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