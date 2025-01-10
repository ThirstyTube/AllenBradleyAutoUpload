using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Diagnostics;
using RockwellAutomation.LogixDesigner;
using System.Runtime.CompilerServices;

class Program
{
    static async Task Main(string[] args)
    {
        // Ensure the SDK server is running
        LogixDesignerSDK.EnsureSDKServerRunning();

        // Execute the backup logic
        await BackupProgram.ExecuteBackupAsync();

        // Ensure the SDK server is stopped at the end
        LogixDesignerSDK.EnsureSDKServerStopped();
    }
}

class LogixDesignerSDK
{
    private const string ProcessName = "Logix Designer SDK Server";
    private const string ExePath = @"C:\\Program Files (x86)\\Rockwell Software\\Studio 5000\\Logix Designer SDK\\LdSdkServer.exe";
    //ProcessName is the name of the SDK server as it appears in windows task manager. the ExePath is where ever you have installed the SDK Package I left mine to the default path. 
    //This information is used to start the SDK Server. and later after use the server will stop 
    public static bool IsSDKServerRunning()
    {
        Process[] processes = Process.GetProcessesByName(ProcessName);
        return processes.Length > 0;
    }

    public static void StartSDKServer()
    {
        try
        {
            if (File.Exists(ExePath))
            {
                Process.Start(ExePath);
                Console.WriteLine("Logix Designer SDK Server started successfully.");
            }
            else
            {
                Console.WriteLine($"SDK Server executable not found at: {ExePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start SDK Server: {ex.Message}");
        }
    }

    public static void StopSDKServer()
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(ProcessName);
            foreach (var process in processes)
            {
                process.Kill();
                Console.WriteLine($"Logix Designer SDK Server process {process.Id} has been stopped.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping SDK Server: {ex.Message}");
        }
    }

    public static void EnsureSDKServerRunning()
    {
        if (!IsSDKServerRunning())
        {
            Console.WriteLine("SDK Server is not running. Starting it now...");
            StartSDKServer();
        }
        else
        {
            Console.WriteLine("SDK Server is already running.");
        }
    }

    public static void EnsureSDKServerStopped()
    {
        if (IsSDKServerRunning())
        {
            Console.WriteLine("Stopping Logix Designer SDK Server...");
            StopSDKServer();
        }
        else
        {
            Console.WriteLine("Logix Designer SDK Server is not running.");
        }
    }
}

class BackupProgram
{
    private static List<(string Folder, string Name, string IP, string Files)> devices = new List<(string Folder, string Name, string IP, string Files)>
    {
        ("C:\\Backups\\Machine1", "Machine1", "192.168.0.21", "CLX"),
        ("C:\\Backups\\Machine2", "Machine2", "192.168.0.22", "CLX"),
        ("C:\\Backups\\Machine2", "Machine2", "192.168.0.23", "COM"),
        //Use this to hard code the folder its dumping to as well as file name. The Name will be concat with todays date so the example would be Machine1_YYYY_MM_DD.ACD 
        //CLX and COM must be present it helps determine communications diffrences between a Control Logix and a CompactLogix. 
    };

    private static bool IsIPReachable(string ip)
    //This will check if PLC is reachable by device running program. 
    {
        try
        {
            using (Ping ping = new Ping())
            {
                PingReply reply = ping.Send(ip, 1000);
                return reply.Status == IPStatus.Success;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ping Error for IP {ip}: {ex.Message}");
            return false;
        }
    }



    private static void ArchiveExistingFiles(string folder)
    //Check if Directory has an "Archive" Folder if not it will create one. 
    {
        try
        {
            string archivePath = Path.Combine(folder, "Archive");

            if (!Directory.Exists(archivePath))
            {
                Directory.CreateDirectory(archivePath);
                Console.WriteLine($"Created Archive folder: {archivePath}");
            }

            string[] acdFiles = Directory.GetFiles(folder, "*.ACD");

            //This for each will search the Main Dir example C:\\Backups\\Machine1\\ it will search for all files ending in .ACD and move them to C:\\Backups\\Machine1\\Archive
            // This helps keep your backup folder organized and the most recent file in the forward most position. 
            foreach (string file in acdFiles)
            {
                string fileName = Path.GetFileName(file);
                string destinationPath = Path.Combine(archivePath, fileName);
                File.Move(file, destinationPath);
                Console.WriteLine($"Archived: {file} -> {destinationPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error archiving files in {folder}: {ex.Message}");
        }
    }

    public static async Task ExecuteBackupAsync()

    //This Loop Starts Uploading the project files from devices in device list. 
    {
        foreach (var device in devices)
        {
            string folder = device.Folder;
            string name = device.Name;

            string ip = device.Files == "CLX"
                ? $"AB_ETH-1\\{device.IP}\\Backplane\\0"
                : $"AB_ETH-1\\{device.IP}";
            //You will nedd to setup a driver in RS Linx that contains all of the IPs to the devices you are going to use with this system. I just use AB_ETH-1 in normal operation. 
            //This is also where the CLX vs COM comes into play since they have two diffrent comm format. ALso 100% of ControlLogix processors are in slot 0 in my workplace.
            //If yours are in diffrent slots? Get Better?
            if (!IsIPReachable(device.IP))
            {
                Console.WriteLine($"Skipping backup for {name} ({device.IP}): IP not reachable.");
                continue;
            }

            string todayDate = DateTime.Now.ToString("yyyy_MM_dd");
            string projectPath = Path.Combine(folder, $"{name}_{todayDate}.ACD");

            ArchiveExistingFiles(folder);

            Console.WriteLine($"Backing up: {projectPath}");
            Console.WriteLine($"Controller IP: {ip}");

            try
            {
                await LogixProject.UploadToNewProjectAsync(projectPath, ip);
                Console.WriteLine("Backup completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Backup Error for {name}: {ex.Message}");
            }
        }
    }
}
