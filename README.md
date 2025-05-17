# Client-Server Application

This is a .NET 8.0-based client-server application that implements a distributed system architecture. The solution consists of three main components: Client, Server, and BusinessLogic.

## Project Structure

```
├── Client/                 # Client application
│   ├── Client.cs          # Main client implementation
│   └── Program.cs         # Client entry point
├── Server/                # Server application
│   ├── Server.cs          # Main server implementation
│   └── Program.cs         # Server entry point
└── BusinessLogic/         # Shared business logic
```

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or Visual Studio Code with C# extensions
- Git (for version control)

## Building the Project

1. Clone the repository:
   ```bash
   git clone https://github.com/bradmasy/COMP-7005-A3.git
   cd COMP-7005-A3
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

## Running the Application

### Starting the Server

1. Navigate to the Server directory:
   ```bash
   cd Server
   ```

2. Run the server with the following arguments:
   ```bash
   dotnet run <ip_address> <port> <delay_before_encryption> <delay_after_encryption>
   ```
   
   Arguments:
   - `ip_address`: The IP address to bind the server to
   - `port`: The port number to listen on
   - `delay_before_encryption`: Delay in milliseconds before encryption
   - `delay_after_encryption`: Delay in milliseconds after encryption

### Starting the Client

1. Open a new terminal window
2. Navigate to the Client directory:
   ```bash
   cd Client
   ```

3. Run the client with the following arguments:
   ```bash
   dotnet run <file_path> <server_ip> <server_port> <encryption_password>
   ```
   
   Arguments:
   - `file_path`: Path to the file to be encrypted and sent
   - `server_ip`: The IP address of the server to connect to
   - `server_port`: The port number of the server
   - `encryption_password`: Password used for encryption

## Project Features

- Client-server communication
- Shared business logic layer
- .NET 8.0 implementation

## Development

The project uses the following technologies and features:
- C# 8.0+
- .NET 8.0
- Nullable reference types
- Implicit using directives

## Testing Instructions

### Testing Multiple Clients (Multiplexing)

To test the server's multiplexing capabilities, you can run multiple clients simultaneously. Here are the commands for different operating systems:

#### For Unix/Mac (using terminal):
```bash
# Run 4 clients simultaneously using & operator
./Client/bin/Debug/net8.0/Client test_files/short_poem.txt 127.0.0.1 8080 password1 &
./Client/bin/Debug/net8.0/Client test_files/tech_story.txt 127.0.0.1 8080 password2 &
./Client/bin/Debug/net8.0/Client test_files/special_chars.txt 127.0.0.1 8080 password3 &
./Client/bin/Debug/net8.0/Client test_files/large_file.txt 127.0.0.1 8080 password4 &
```

#### For Windows (using Command Prompt):
```batch
# Run 4 clients simultaneously using start command
start Client\bin\Debug\net8.0\Client.exe test_files\short_poem.txt 127.0.0.1 8080 password1
start Client\bin\Debug\net8.0\Client.exe test_files\tech_story.txt 127.0.0.1 8080 password2
start Client\bin\Debug\net8.0\Client.exe test_files\special_chars.txt 127.0.0.1 8080 password3
start Client\bin\Debug\net8.0\Client.exe test_files\large_file.txt 127.0.0.1 8080 password4
```

Note: Do NOT use `&&` in Windows as it runs commands sequentially, not simultaneously.

#### Alternative Method (Using Multiple Terminal Windows):
1. Start the server in one terminal window
2. Open 4 additional terminal windows
3. Run a different client in each window:
   ```bash
   # Terminal 1
   ./Client/bin/Debug/net8.0/Client test_files/short_poem.txt 127.0.0.1 8080 password1

   # Terminal 2
   ./Client/bin/Debug/net8.0/Client test_files/tech_story.txt 127.0.0.1 8080 password2

   # Terminal 3
   ./Client/bin/Debug/net8.0/Client test_files/special_chars.txt 127.0.0.1 8080 password3

   # Terminal 4
   ./Client/bin/Debug/net8.0/Client test_files/large_file.txt 127.0.0.1 8080 password4
   ```

### Expected Behavior
When running multiple clients simultaneously:
1. All clients will connect to the server quickly
2. The server will add all clients to its connection list
3. The server will process each client's request sequentially
4. Each client will receive its decrypted file back

Note: The server processes clients one at a time, so if each request takes 5 seconds, 4 clients will take approximately 20 seconds total to process.

### Test Files
The `test_files` directory contains several test files for different scenarios:
- `short_poem.txt`: Small file for basic testing
- `tech_story.txt`: Medium-sized file for testing
- `large_file.txt`: Large file with repeated content
- `empty_file.txt`: Empty file for edge case testing
- `special_chars.txt`: File with special characters and Unicode
