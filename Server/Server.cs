using System.Net;
using System.Net.Sockets;
using System.Text;
using BusinessLogic;
using static BusinessLogic.Constants;

namespace Server;

public class Server(string ipAddress, int port, int delayBeforeSec, int delayAfterSec)
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

                    DisplayMessage($"Adding new client: {clientSocket.Handle}");
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

    private async Task<byte[]> ReceiveAndDecryptedData(byte[] buffer, int size)
    {
        var data = Encoding.ASCII.GetString(buffer, 0, size);

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

    private async Task HandleClient(Socket sock)
    {
        var bufferSize = sock.Available > 0 ? sock.Available : 1024;
        var buffer = new byte[bufferSize];

        try
        {
            var received = await sock.ReceiveAsync(buffer, SocketFlags.None);

            // Throw error if no data is received
            if (received == NoDataSent) throw new Exception("No data received");

            DisplayMessage($"Connection has received {bufferSize} bytes.");

            var decryptedFileBytes = await ReceiveAndDecryptedData(buffer, bufferSize);

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

    private async Task AddDelay(int time)
    {
        await Task.Delay(time);
    }
}