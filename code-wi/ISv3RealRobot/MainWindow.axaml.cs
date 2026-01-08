using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace InventorySystem;

public partial class MainWindow : Window
{
    private ItemSorterRobot? _robot;

    public MainWindow()
    {
        InitializeComponent();

        StatusMessages.Text =
            "Klar. Tryk Connect, derefter Run.\n" +
            "Regel: sensor => BIG/SMALL (sortér i 2 bunker).\n";
    }

    // Button Click handler (skal have object? + RoutedEventArgs)
    public async void ConnectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var ip = IpAddress.Text?.Trim();
            if (string.IsNullOrWhiteSpace(ip))
                ip = "localhost";

            _robot = new ItemSorterRobot(ip);

            await _robot.ConnectAsync();

            StatusMessages.Text += $"Connected: {_robot.Connected}\n";
        }
        catch (Exception ex)
        {
            StatusMessages.Text += $"Fejl ved connect: {ex.Message}\n";
        }
    }

    // Button Click handler (skal have object? + RoutedEventArgs)
    public async void ProcessButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_robot is null || !_robot.Connected)
        {
            StatusMessages.Text += "Ikke connected. Tryk Connect først.\n";
            return;
        }

        try
        {
            StatusMessages.Text += "Starter: 3 pick-positioner (0,1,2)\n";

            // Kør 3 forskellige pick-positioner: 0,1,2
            for (uint pickIndex = 0; pickIndex < 3; pickIndex++)
            {
                StatusMessages.Text += $"Kører pos {pickIndex + 1} (id={pickIndex})...\n";
                await _robot.PickUp(pickIndex);
            }

            StatusMessages.Text += "Done.\n";
        }
        catch (Exception ex)
        {
            StatusMessages.Text += $"Fejl under Run: {ex.Message}\n";
        }
    }
}

/// <summary>
/// Robot der sender URScript-template fra filen: move-items-to-shipment-box.script
/// Filen skal ligge i projektet og kopieres til output (det gør du i .csproj).
/// </summary>
public class ItemSorterRobot : Robot
{
    public const string TemplateFileName = "move-items-to-shipment-box.script";

    public ItemSorterRobot(string ipAddress) : base(ipAddress)
    {
    }

    private string LoadProgram()
    {
        // Læs fra output-mappen (bin/Debug/...),
        // fordi .csproj kopierer scriptet derud.
        var path = Path.Combine(AppContext.BaseDirectory, TemplateFileName);
        return File.ReadAllText(path) + Environment.NewLine;
    }

    public async Task PickUp(uint itemId)
    {
        var program = LoadProgram();

        // itemId bliver sat ind i {0} i URScript-template
        var script = string.Format(CultureInfo.InvariantCulture, program, itemId);

        SendUrscript(script);

        // Vent mens robotten kører programmet
        while (ProgramRunning)
            await Task.Delay(100);
    }
}
