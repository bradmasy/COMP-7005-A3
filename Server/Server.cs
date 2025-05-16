using System.Net;
using System.Net.Sockets;
using System.Text;
using BusinessLogic;
using static BusinessLogic.Constants;

namespace Server;

public class Server(string ipAddress, int port)
{
    private const int Timeout = 100000;
    private static readonly byte[] Buffer = new byte[ByteArraySize];
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
                    Console.WriteLine("Adding socket to clients");
                }

                foreach (var sock in _clients)
                {
                    // check if closed
                    // if the client socket is ready to read, follow with the processing...
                    // tip: this could be where a new thread executes the processing after the poll...

                    if (sock.Poll(0, SelectMode.SelectRead))
                    {
                        var bufferSize = sock.Available > 0 ? sock.Available : 1024;

                        var buffer = new byte[bufferSize];
                        var received = await sock.ReceiveAsync(buffer, SocketFlags.None);

                        // Close if there is no incoming data
                        if (received == NoDataSent) sock.Close();

                        var data = Encoding.ASCII.GetString(buffer, 0, bufferSize);
                        var decryptedFile = DecryptPayloadToData(data);

                        Console.WriteLine(decryptedFile);
                        // open file / create a file


                        // process file
                        // return results
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
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

    private static void Flush()
    {
        Buffer.AsSpan().Clear();
    }

    private static async Task HandleClient(Socket clientSocket)
    {
        Console.WriteLine("Client connected");
        try
        {
            var message = await Receive(clientSocket);
            var processed = ProcessMessage(message);
            var encoded = EncryptionService.Encrypt(processed[Message], processed[Password]);

            await Send(clientSocket, encoded);

            var success = ConstructSuccess(processed[Message], Encoding.ASCII.GetString(encoded));
            DisplayMessage(success);
        }
        catch (Exception ex)
        {
            var errorResponse = CreateErrorMessage(ex.Message);
            await Send(clientSocket, errorResponse);
            DisplayMessage(ex.Message);
        }
        finally
        {
            EndClientSession(clientSocket);
            Flush();
        }
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

    private static async Task<string> Receive(Socket client)
    {
        var received = await client.ReceiveAsync(Buffer, SocketFlags.None);
        if (received == 0)
        {
            throw new Exception("Client disconnected unexpectedly");
        }

        return Encoding.UTF8.GetString(Buffer, 0, received);
    }
}