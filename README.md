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
