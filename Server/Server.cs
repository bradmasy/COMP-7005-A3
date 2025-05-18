using System.Net;
using System.Net.Sockets;
using System.Text;
using BusinessLogic;
using static BusinessLogic.Constants;

namespace Server;

public class Server
{
    private readonly Socket _serverSocket;
    private readonly List<Socket> _clients;
    private readonly string _ipAddress;
    private readonly int _port;
    private readonly int _delayBeforeSec;
    private readonly int _delayAfterSec;

    public Server(string ipAddress, int port, int delayBeforeSec, int delayAfterSec)
    {
        _ipAddress = ipAddress;
        _port = port;
        _delayBeforeSec = delayBeforeSec;
        _delayAfterSec = delayAfterSec;
        _ipAddress = ipAddress;
        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _clients = [_serverSocket];
    }

    public async Task Run()
    {
        try
        {
            BindAndListen();

            DisplayMessage($"Server is bound to socket: {_serverSocket.LocalEndPoint}");

            while (true)
            {
                if (_clients[ServerSocket].Poll(0, SelectMode.SelectRead))
                {
                    var clientSocket = await _clients[ServerSocket].AcceptAsync();
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
            DisplayMessage(ex.Message);
        }
    }

    private async Task<byte[]> ReceiveAndDecryptedData(byte[] buffer)
    {
        var data = Encoding.ASCII.GetString(buffer, 0, buffer.Length);

        DisplayMessage("About to decrypt message...");

        await AddDelay(_delayBeforeSec);

        var decryptedFile = DecryptPayloadToData(data);

        await AddDelay(_delayAfterSec);

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
        _clients[ServerSocket].Close();
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

        if (totalLength is <= 0 or > int.MaxValue)
        {
            throw new Exception(
                $"Invalid data length: {totalLength} bytes. Maximum allowed size is {int.MaxValue} bytes");
        }

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
        try
        {
            var received = await ReadIncomingDataStream(sock);
            DisplayMessage($"Connection has received {received.Length} bytes.");

            var decryptedFileBytes = await ReceiveAndDecryptedData(received);
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
            // failsafe if the socket has disconnected
            if (sock.Connected)
            {
                DisplayMessage("Closing socket");
                EndClientSession(sock);
            }

            _clients.Remove(sock);
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
        if (messageParts.Length < ExpectedMessages) throw new Exception($"Invalid message");
        return messageParts;
    }

    private void BindAndListen()
    {
        // Set socket option before binding
        _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        var endpoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
        _serverSocket.Bind(endpoint);
        _serverSocket.Listen(Connections);
    }

    private async Task AddDelay(int time)
    {
        var wait = time * MilliSeconds;
        await Task.Delay(wait);
    }
}