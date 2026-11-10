using Microsoft.Extensions.Configuration;

public class AppSettings
{
    public string Servidor { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string SerialWindows { get; set; } = "";
    public string SerialLinux { get; set; } = "";
    public int SerialBaudRate { get; set; } = 0;
    public int IntervaloGapSerialMiliSegundos { get; set; } = 0;
    public int DebugOut_PosData { get; set; } = 0;
}

