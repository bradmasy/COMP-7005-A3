using System.Net;
using System.Net.Sockets;
using System.Text;
using BusinessLogic;
using static BusinessLogic.Constants;

namespace Server;

public class Server(string ipAddress, int port, int delayBeforeSec, int delayAfterSec)
{
    private readonly Socket _serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly List<Socket> _clients = [];

    public async Task Run()
    {
        try
        {
            BindAndListen();

            DisplayMessage($"Server is bound to socket: {_serverSocket.LocalEndPoint}");

            while (true)
            {
                if (_serverSocket.Poll(0, SelectMode.SelectRead))
                {
                    var clientSocket = await _serverSocket.AcceptAsync();
                    _clients.Add(clientSocket);

                    DisplayMessage($"Adding new client: {clientSocket.Handle}");
                }

                foreach (var sock in _clients.ToList())
                {
                    if (sock.Poll(0, SelectMode.SelectRead))
                    {
                        await HandleClient(sock);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task<byte[]> ReceiveAndDecryptedData(byte[] buffer, int size)
    {
        var data = Encoding.ASCII.GetString(buffer, 0, buffer.Length);

        DisplayMessage("About to decrypt message...");

        await AddDelay(delayBeforeSec);

        var decryptedFile = DecryptPayloadToData(data);

        await AddDelay(delayAfterSec);

        DisplayMessage("decryption complete after delays...");

        return Encoding.ASCII.GetBytes(decryptedFile);
    }

    private static string DecryptPayloadToData(string data)
    {
        var splitData = ProcessMessage(data);
        var password = splitData[RawPassword];
        var encryptedFile = splitData[EncryptedFileData];
        var decrypted = EncryptionService.Decrypt(encryptedFile, password);
        return decrypted;
    }

    public void TearDown()
    {
        _serverSocket.Close();
    }

    private static void Flush(byte[] buffer)
    {
        buffer.AsSpan().Clear();
    }

    private static async Task<byte[]> ReadIncomingDataStream(Socket sock)
    {
        var lengthBuffer = new byte[SizePrefix];
        var lengthBytesReceived = await sock.ReceiveAsync(lengthBuffer, SocketFlags.None);

        if (lengthBytesReceived != SizePrefix)
        {
            throw new Exception("Failed to receive length prefix");
        }

        var totalLength = BitConverter.ToInt64(lengthBuffer, 0);
        var dataBuffer = new byte[totalLength];

        var totalBytesReceived = 0;

        // Read until we get all the data
        while (totalBytesReceived < totalLength)
        {
            var received = await sock.ReceiveAsync(dataBuffer.AsMemory(totalBytesReceived), SocketFlags.None);
            if (received == 0) break; // Connection closed by client
            totalBytesReceived += received;
        }

        if (totalBytesReceived == 0) throw new Exception("No data received");
        if (totalBytesReceived != totalLength)
            throw new Exception(
                $"Incomplete data received. Expected {totalLength} bytes but got {totalBytesReceived} bytes");

        return dataBuffer;
    }

    private async Task HandleClient(Socket sock)
    {
        var bufferSize = sock.Available > 0 ? sock.Available : 1024;

        var buffer = new byte[bufferSize];

        try
        {
            var received = await ReadIncomingDataStream(sock); //await sock.ReceiveAsync(buffer, SocketFlags.None);

            DisplayMessage($"Connection has received {received.Length} bytes.");

            var decryptedFileBytes = await ReceiveAndDecryptedData(received, bufferSize);

            DisplayMessage("Sending decrypted data back to client...");

            await Send(sock, decryptedFileBytes);
        }
        catch (Exception ex)
        {
            var errorBytes = CreateErrorMessage(ex.Message);

            await Send(sock, errorBytes);
        }
        finally
        {
            EndClientSession(sock);
            _clients.Remove(sock);

            Flush(buffer);
        }
    }

    private static void DisplayMessage(string message)
    {
        Console.WriteLine(message);
    }

    private static void EndClientSession(Socket clientSocket)
    {
        clientSocket.Close();
        clientSocket.Dispose();
    }

    private static byte[] CreateErrorMessage(string message)
    {
        return Encoding.ASCII.GetBytes($"ERROR: {message}");
    }

    private static async Task Send(Socket client, byte[] data)
    {
        var totalSent = 0;
        while (totalSent < data.Length)
        {
            var sent = await client.SendAsync(data.AsMemory(totalSent), SocketFlags.None);

            if (sent <= NoBytes)
            {
                throw new Exception("Error transmitting message to client.");
            }

            totalSent += sent;
        }
    }

    private static string[] ProcessMessage(string message)
    {
        var messageParts = message.Split(Delimiter);
        if (messageParts.Length < ExpectedMessages) throw new Exception($"Invalid message: {message}");
        return messageParts;
    }

    private void BindAndListen()
    {
        // Set socket option before binding
        _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        _serverSocket.Bind(endpoint);
        _serverSocket.Listen(Connections);
    }

    private async Task AddDelay(int time)
    {
        var wait = time * MilliSeconds;
        await Task.Delay(wait);
    }
}