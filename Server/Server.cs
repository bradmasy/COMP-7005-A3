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
                    // If the read event is not ready, skip
                    // tip: this could be where a new thread executes the processing after the poll...
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

    private async Task<byte[]> ReadIncomingDataStream(Socket sock)
    {
        const int bufferSize = 1024; // 1KB buffer

        var buffer = new byte[bufferSize];
        var chunks = new List<byte[]>();
        var totalBytesReceived = 0;

        while (true)
        {
            var received = await sock.ReceiveAsync(buffer, SocketFlags.None);

            if (received == 0) break; // Connection closed by client

            var chunk = new byte[received];
            Array.Copy(buffer, chunk, received);
            chunks.Add(chunk);
            totalBytesReceived += received;

            // If we received less than the buffer size, we've got all the data
            if (received < bufferSize) break;
        }

        if (totalBytesReceived == NoDataSent) throw new Exception("No data received");

        var completeData = new byte[totalBytesReceived];
        var offset = 0;

        foreach (var chunk in chunks)
        {
            Array.Copy(chunk, 0, completeData, offset, chunk.Length);
            offset += chunk.Length;
        }

        return completeData;
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
            if (sock.Connected)
            {
                Console.WriteLine("Closing socket");
                EndClientSession(sock);
            }
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

    // private static async Task Send(Socket client, byte[] data)
    // {
    //     Console.WriteLine($"Sending data to client...{data.Length}");
    //     var descriptor = await client.SendAsync(data, SocketFlags.None);
    //     if (descriptor <= 0)
    //     {
    //         throw new Exception("Error transmitting message to client.");
    //     }
    // }

    private static async Task Send(Socket client, byte[] data)
    {
        Console.WriteLine($"Sending data to client...{data.Length}");
        int totalSent = 0;
        while (totalSent < data.Length)
        {
            int sent = await client.SendAsync(data.AsMemory(totalSent), SocketFlags.None);
            if (sent <= 0)
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