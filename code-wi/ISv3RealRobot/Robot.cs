using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace InventorySystem;

public class Robot
{
    private readonly string _ip;
    private TcpClient? _client;
    private NetworkStream? _stream;

    public bool Connected => _client?.Connected == true;
    public bool ProgramRunning { get; private set; }
    public string RobotMode { get; private set; } = "UNKNOWN";

    public Robot(string ipAddress = "localhost")
    {
        _ip = ipAddress;
    }

    public async Task ConnectAsync()
    {
        _client = new TcpClient();
        await _client.ConnectAsync(_ip, 30002);
        _stream = _client.GetStream();
        await UpdateRobotModeAsync();
    }

    public void SendUrscript(string program)
    {
        if (_stream is null)
            throw new InvalidOperationException("Robot ikke forbundet (30002).");

        var bytes = Encoding.ASCII.GetBytes(program + "\n");
        try
        {
            _stream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception ex)
        {
            throw new Exception("Kunne ikke sende URScript til robot. Tjek IP/port 30002. " + ex.Message, ex);
        }


        // simpel “running” indikator
        ProgramRunning = true;
        _ = Task.Run(async () =>
        {
            await Task.Delay(1800);
            ProgramRunning = false;
        });
    }

    public async Task PowerOnAsync()
    {
        await TryDashboardCommandAsync("power on");
        await UpdateRobotModeAsync();
    }

    public async Task BrakeReleaseAsync()
    {
        await TryDashboardCommandAsync("brake release");
        await UpdateRobotModeAsync();
    }

    private async Task UpdateRobotModeAsync()
    {
        var reply = await TryDashboardCommandAsync("robotmode");
        if (!string.IsNullOrWhiteSpace(reply))
            RobotMode = reply.Trim();
    }

    private async Task<string?> TryDashboardCommandAsync(string cmd)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_ip, 29999);
            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
            using var reader = new StreamReader(stream, Encoding.ASCII);

            await writer.WriteLineAsync(cmd);
            return await reader.ReadLineAsync();
        }
        catch
        {
            return null;
        }
    }
}

