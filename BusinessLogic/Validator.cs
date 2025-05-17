using System.Net;
using System.Text.RegularExpressions;
using static BusinessLogic.Constants;

namespace BusinessLogic;

public static class Validator
{
    private static readonly Regex Ipv4Regex = new Regex(
        @"^(25[0-5]|2[0-4]\d|1\d{2}|[0-9]?\d)" +
        @"(\.(25[0-5]|2[0-4]\d|1\d{2}|[0-9]?\d)){3}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public static void ValidateServerArgs(string[] args)
    {
        switch (args.Length)
        {
            case NoArgs:
                throw new Exception("Please provide an IP address and Port number.");
            case < AmountOfServerArgs:
                throw new Exception("Please provide four valid arguments. The IP address, port, delay before and after encryption.");
            case > AmountOfServerArgs:
                throw new Exception("Too many arguments provided.");
        }

        if (string.IsNullOrEmpty(args[IpAddress]) || string.IsNullOrWhiteSpace(args[IpAddress]))
        {
            throw new Exception("Null or empty IP Address provided. Please try again.");
        }

        if (!Ipv4Regex.IsMatch(args[IpAddress]))
        {
            throw new Exception("Invalid IP Address provided. Please try again.");
        }


        if (!int.TryParse(args[Port], out var port) || port < MinPort || port > MaxPort)
        {
            throw new ArgumentException("Invalid port number. Please enter a number between 1 and 65535.");
        }
        
        if (!int.TryParse(args[ConfigurableDelayBefore], out var delayBeforeSeconds))
        {
            throw new ArgumentException("Invalid number. Please enter a valid integer for the delay before.");
        }

        if (!int.TryParse(args[ConfigurableDelayAfter], out var delayAfterSeconds))
        {
            throw new ArgumentException("Invalid number. Please enter a valid integer for the delay after.");
        }

    }


    public static void ValidateClientArguments(string[] args)
    {
        switch (args.Length)
        {
            case NoArgs:
                throw new Exception("no arguments provided.");
            case < MaxClientArgs:
                throw new Exception("Invalid number of arguments provided. Please try again.");
        }

        if (!IPAddress.TryParse(args[IpAddress], out _))
        {
            throw new Exception("Invalid IP Address provided. Please try again.");
        }

        if (!int.TryParse(args[Port], out var port) || port < MinPort || port > MaxPort)
        {
            throw new ArgumentException("Invalid port number. Please enter a number between 1 and 65535.");
        }
    }
}