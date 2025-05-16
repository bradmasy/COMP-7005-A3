using System.Net;
using System.Net.Sockets;
using System.Text;
using BusinessLogic;
using static BusinessLogic.Constants;

namespace Server;

public class Server(string ipAddress, int port)
{
    private const int Timeout = 100000;
    private readonly Socket _serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly List<Socket> _clients = [];

    public async Task Run()
    {
        try
        {
            BindAndListen();

            Console.WriteLine($"Server is bound to socket: {_serverSocket.LocalEndPoint}");

            while (true)
            {
                if (_serverSocket.Poll(Timeout, SelectMode.SelectRead))
                {
                    var clientSocket = await _serverSocket.AcceptAsync();
                    _clients.Add(clientSocket);

                    Console.WriteLine($"Adding new client: {clientSocket.Handle}");
                }

                foreach (var sock in _clients.ToList())
                {
                    // If the read event is not ready, skip
                    // tip: this could be where a new thread executes the processing after the poll...

                    if (!sock.Poll(0, SelectMode.SelectRead)) continue;

                    await HandleClient(sock);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static byte[] ReceiveAndDecryptedData(byte[] buffer, int size)
    {
        var data = Encoding.ASCII.GetString(buffer, 0, size);
        var decryptedFile = DecryptPayloadToData(data);
        return Encoding.ASCII.GetBytes(decryptedFile);
    }

    private static string DecryptPayloadToData(string data)
    {
        var splitData = data.Split("|");

        if (splitData.Length != ExpectedPayload) throw new Exception("Illegal payload. No delimiter found.");

        var password = splitData[RawPassword];
        var encryptedFile = splitData[EncryptedFileData];

        var decrypted = EncryptionService.Decrypt(encryptedFile, password);
        return decrypted;
    }

    public void TearDown()
    {
        _serverSocket.Close();
    }

    private async Task PollClients()
    {
        if (_serverSocket.Poll(Timeout, SelectMode.SelectRead))
        {
            var clientSocket = await _serverSocket.AcceptAsync();
            _clients.Add(clientSocket);
        }
    }

    private static void Flush(byte[] buffer)
    {
        buffer.AsSpan().Clear();
    }

    private async Task HandleClient(Socket sock)
    {
        var bufferSize = sock.Available > 0 ? sock.Available : 1024;
        var buffer = new byte[bufferSize];
        var received = await sock.ReceiveAsync(buffer, SocketFlags.None);

        // Close if there is no incoming data
        if (received == NoDataSent) throw new Exception("No data received");

        var decryptedFileBytes = ReceiveAndDecryptedData(buffer, bufferSize);

        await sock.SendAsync(decryptedFileBytes, SocketFlags.None);

        sock.Close();
        sock.Dispose();

        _clients.Remove(sock);
        Flush(buffer);
    }

    private static void DisplayMessage(string message)
    {
        Console.WriteLine(message);
    }

    private static string ConstructSuccess(string message, string encoded)
    {
        return $"Message processed | Original: [{message}] Encoded:[{encoded}]";
    }

    private static void EndClientSession(Socket clientSocket)
    {
        clientSocket.Close();
    }

    private static byte[] CreateErrorMessage(string message)
    {
        return Encoding.UTF8.GetBytes($"ERROR: {message}");
    }

    private static async Task Send(Socket client, byte[] data)
    {
        var descriptor = await client.SendAsync(data, SocketFlags.None);
        if (descriptor <= 0)
        {
            throw new Exception("Error transmitting message to client.");
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
}