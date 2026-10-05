using System.Diagnostics;
using System.Text.RegularExpressions;
using Vortice.DirectInput; // dotnet add package Vortice.DirectInput

namespace iRacingWheelSync;

public static class Program
{
    private static readonly IDirectInput8 Dinput = DInput.DirectInput8Create();

    public static void Main()
    {
        var docsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "iRacing", "profiles", "controls", "Baseline");

        var joyCalibPath = Path.Combine(docsPath, "joyCalib.yaml");

        var controlsPath = Path.Combine(docsPath, "controls.cfg");

        var wasRunningWarningShown = false;

        var lastState = "";

        while (true)
        {
            Thread.Sleep(3000);

            try
            {
                var isIRacingRunning = Process.GetProcesses().Any(p =>
                    p.ProcessName.Equals("iRacingSim", StringComparison.OrdinalIgnoreCase) ||
                    p.ProcessName.Equals("iRacingSim64DX11", StringComparison.OrdinalIgnoreCase) ||
                    p.ProcessName.Equals("iRacingUI", StringComparison.OrdinalIgnoreCase));

                if (isIRacingRunning)
                {
                    if (!wasRunningWarningShown)
                    {
                        Console.WriteLine("iRacing running, close the UI (interface) and/or the simulator.");

                        wasRunningWarningShown = true;

                        lastState = "";
                    }

                    continue;
                }

                wasRunningWarningShown = false;

                if (!File.Exists(joyCalibPath) || !File.Exists(controlsPath))
                {
                    if (lastState != "FilesNotFound")
                    {
                        Console.WriteLine("iRacing configuration files not found.");

                        lastState = "FilesNotFound";
                    }

                    continue;
                }

                var joyCalibContent = File.ReadAllText(joyCalibPath);

                var activeProductGuid = GetActiveProductGuid(joyCalibContent);

                if (activeProductGuid == null)
                {
                    if (lastState != "WheelNotFound")
                    {
                        Console.WriteLine("Wheel connection was not found in your pc or is not configured in iRacing. No changes were made.");

                        lastState = "WheelNotFound";
                    }

                    continue;
                }

                var targetProductGuidStr = activeProductGuid.Value.ToString().ToUpper();

                var currentInstanceGuid = GetCurrentInstanceGuid(activeProductGuid.Value);

                if (currentInstanceGuid == null)
                {
                    if (lastState != "InstanceError")
                    {
                        Console.WriteLine("Could not retrieve InstanceGUID for the active wheel.");

                        lastState = "InstanceError";
                    }

                    continue;
                }

                var configuredGuid = GetConfiguredInstanceGuid(joyCalibContent, targetProductGuidStr);

                if (configuredGuid == null)
                {
                    if (lastState != "ConfigNotFound")
                    {
                        Console.WriteLine("Wheel not found in iRacing configurations (joyCalib.yaml).");

                        lastState = "ConfigNotFound";
                    }

                    continue;
                }

                if (currentInstanceGuid.Value == configuredGuid.Value)
                {
                    if (lastState != "Equal")
                    {
                        Console.WriteLine($"Wheel found! Current InstanceGUID: {{{currentInstanceGuid.Value.ToString().ToUpper()}}}\n");

                        Console.WriteLine($"Configured GUID in iRacing: {{{configuredGuid.Value.ToString().ToUpper()}}}\n");

                        Console.WriteLine("GUIDs are equal. No synchronization required.");

                        lastState = "Equal";
                    }

                    continue;
                }

                Console.WriteLine($"Wheel found! Current InstanceGUID: {{{currentInstanceGuid.Value.ToString().ToUpper()}}}\n");

                Console.WriteLine($"Configured GUID in iRacing: {{{configuredGuid.Value.ToString().ToUpper()}}}\n");

                Console.WriteLine("Different GUIDs! Starting migration in the configuration files...");

                var oldGuidStr = configuredGuid.Value.ToString().ToUpper();

                var newGuidStr = currentInstanceGuid.Value.ToString().ToUpper();

                var updatedJoyCalib = joyCalibContent.Replace(oldGuidStr, newGuidStr, StringComparison.OrdinalIgnoreCase);

                File.WriteAllText(joyCalibPath, updatedJoyCalib);

                Console.WriteLine("-> joyCalib.yaml updated with success.");

                var controlsBytes = File.ReadAllBytes(controlsPath);

                var oldGuidBytes = configuredGuid.Value.ToByteArray();

                var newGuidBytes = currentInstanceGuid.Value.ToByteArray();

                var updatedControlsBytes = ReplaceBytes(controlsBytes, oldGuidBytes, newGuidBytes);

                File.WriteAllBytes(controlsPath, updatedControlsBytes);

                Console.WriteLine("-> controls.cfg updated with success.");

                Console.WriteLine("Update concluded! Now your wheel will be recognized by iRacing.");

                lastState = "Updated";
            }
            catch (Exception ex)
            {
                if (lastState != "Error")
                {
                    Console.WriteLine($"Erro no Main: {ex.Message}");

                    lastState = "Error";
                }
            }
        }
    }


    private static Guid? GetActiveProductGuid(string yamlContent)
    {
        var devices = Dinput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);

        foreach (var device in devices)
        {
            var productGuidStr = device.ProductGuid.ToString().ToUpper();

            var pattern = $@"ProductGUID:\s*'{{{productGuidStr}}}'";

            if (Regex.IsMatch(yamlContent, pattern, RegexOptions.IgnoreCase))
            {
                return device.ProductGuid;
            }
        }

        return null;
    }


    private static Guid? GetCurrentInstanceGuid(Guid targetProductGuid)
    {
        var devices = Dinput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);

        foreach (var device in devices)
        {
            if (device.ProductGuid == targetProductGuid)
            {
                return device.InstanceGuid;
            }
        }

        return null;
    }


    private static Guid? GetConfiguredInstanceGuid(string yamlContent, string productGuidStr)
    {
        var pattern = $@"InstanceGUID:\s*'{{(?<guid>[A-Fa-f0-9\-]+)}}'\s*[\r\n]+.*ProductGUID:\s*'{{{productGuidStr}}}'";

        var match = Regex.Match(yamlContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (match.Success && Guid.TryParse(match.Groups["guid"].Value, out var parsedGuid))
        {
            return parsedGuid;
        }

        return null;
    }


    private static byte[] ReplaceBytes(byte[] source, byte[] search, byte[] replacement)
    {
        var result = new List<byte>(source.Length);

        var i = 0;

        while (i < source.Length)
        {
            if (IsMatch(source, i, search))
            {
                result.AddRange(replacement);

                i += search.Length;
            }
            else
            {
                result.Add(source[i]);

                i++;
            }
        }

        return result.ToArray();
    }


    private static bool IsMatch(byte[] source, int position, byte[] search)
    {
        if (position + search.Length > source.Length)
        {
            return false;
        }

        for (var i = 0; i < search.Length; i++)
        {
            if (source[position + i] != search[i])
            {
                return false;
            }
        }

        return true;
    }
}