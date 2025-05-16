using System.Text;
using static BusinessLogic.Constants;

namespace BusinessLogic;

public static class EncryptionService
{
    public static byte[] Encrypt(string data, string password)
    {
        var encrypted = VigenereCipher(data, password);
        return Encoding.UTF8.GetBytes(encrypted);
    }

    public static string EncryptToString(string data, string password)
    {
        return VigenereCipher(data, password);
    }

    private static string VigenereCipher(string message, string password)
    {
        var builder = new StringBuilder();

        var shiftArray = password.ToUpper().ToCharArray().Select(c => c - 'A').ToArray();

        var shiftIndex = 0;

        foreach (var letter in message)
        {
            if (char.IsLetter(letter))
            {
                var offset = char.IsUpper(letter) ? UpperAscii : LowerAscii;
                var shift = shiftArray[shiftIndex];

                var encryptedChar = (char)(((letter - offset + shift) % AsciiShift) + offset);
                builder.Append(encryptedChar);

                shiftIndex = (shiftIndex + 1) % shiftArray.Length;
            }
            else
            {
                builder.Append(letter);
            }
        }

        return builder.ToString();
    }

    public static string Decrypt(string encryptedMessage, string password)
    {
        var builder = new StringBuilder();
        var shiftArray = password.ToUpper().ToCharArray().Select(c => c - 'A').ToArray();

        var shiftIndex = 0;

        foreach (var letter in encryptedMessage)
        {
            if (char.IsLetter(letter))
            {
                var offset = char.IsUpper(letter) ? 'A' : 'a';
                var shift = shiftArray[shiftIndex];

                var decryptedChar = (char)(((letter - offset - shift + 26) % 26) + offset);
                builder.Append(decryptedChar);

                shiftIndex = (shiftIndex + 1) % shiftArray.Length;
            }
            else
            {
                builder.Append(letter);
            }
        }

        return builder.ToString();
    }
}