using System.Text.RegularExpressions;

namespace EyeVision;

public static class StringExtensions
{
    public static string ToValidFileName(this string? input)
    {
        // Replace invalid file name characters with an underscore
        string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        string invalidRegex = $"[{invalidChars}]";
        string formatted = Regex.Replace(input, invalidRegex, "_");

        // Trim leading or trailing dots and spaces
        return formatted.Trim().Trim('.');
    }
}