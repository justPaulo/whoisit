using System.Diagnostics;

namespace WhoIsIt;

public static class ClipboardHelper
{
    public static bool CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        try
        {
            if (OperatingSystem.IsMacOS())
            {
                return CopyToClipboardMacOS(text);
            }
            else if (OperatingSystem.IsWindows())
            {
                return CopyToClipboardWindows(text);
            }
            else if (OperatingSystem.IsLinux())
            {
                return CopyToClipboardLinux(text);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CopyToClipboardMacOS(string text)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "pbcopy",
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
            return false;

        process.StandardInput.Write(text);
        process.StandardInput.Close();
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    private static bool CopyToClipboardWindows(string text)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "clip",
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
            return false;

        process.StandardInput.Write(text);
        process.StandardInput.Close();
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    private static bool CopyToClipboardLinux(string text)
    {
        // Try xclip first
        var processInfo = new ProcessStartInfo
        {
            FileName = "xclip",
            Arguments = "-selection clipboard",
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            // If xclip fails, try xsel as fallback
            return TryXsel(text);
        }
    }

    private static bool TryXsel(string text)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "xsel",
                Arguments = "--clipboard --input",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
