using System.IO.Ports;
using TetraPosTraccar;
using System.Globalization;
using System.Collections.Concurrent;
public class SerialRadio : IDisposable
{
    private static SerialPort? serialport;   
    private static string dados = string.Empty;
    static System.Timers.Timer tmrRxSerial;
    DateTime lastUsbReceive;

    TetraPDUDecoder pduDecoder = new TetraPDUDecoder();     // antes de fazer chamada: TetraPDUDecoderInit.TetraPDUDecoderInit();
    public ConcurrentQueue<Globals.PosDataReceived> filaMessages = new ConcurrentQueue<Globals.PosDataReceived>();
    MessageQueuing? messageQueuing;

    public void SerialPortConfig()
    {      
        // TODO: Try Catch
        serialport = new SerialPort
        {
            PortName = OperatingSystem.IsWindows() ? Program.opcoes.SerialWindows : Program.opcoes.SerialLinux,
            BaudRate = Program.opcoes.SerialBaudRate, /* Program.opcoes acoplado mas... TODO: desacoplar*/
            DataBits = 8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            Handshake = Handshake.None,
            ReadTimeout = 10,
            NewLine = "\r\n"
        };
        tmrRxSerial = new System.Timers.Timer(250);
        tmrRxSerial.Elapsed += new System.Timers.ElapsedEventHandler(TmrRxSerial_Elapsed);
        tmrRxSerial.Enabled = false;

        serialport.DataReceived += SerialPortRx;
        serialport.Open();

        messageQueuing = new MessageQueuing(filaMessages);

        Console.WriteLine("> Serial Open OK");
    }
    private void SerialPortRx(object sender, SerialDataReceivedEventArgs e)
    {
        dados += serialport!.ReadExisting();
        lastUsbReceive = DateTime.Now;
        if (tmrRxSerial.Enabled == false)
            tmrRxSerial.Enabled = true;      

 //       Console.Write("> SerialRx: " + dados);        
    }
    public void TmrRxSerial_Elapsed(object sender, EventArgs e)
    {
        DateTime tempo_a = DateTime.Now;

        if (lastUsbReceive.AddMilliseconds(Program.opcoes.IntervaloGapSerialMiliSegundos) <= tempo_a)
        {
            AnalisaSerialRX();

            tmrRxSerial.Enabled = false;
        }

    }
    private void AnalisaSerialRX()
    { 
        if (!dados.Contains("+CTSDSR"))
        {
            dados = string.Empty;
            return;
        }
            
        string[] _Messages = dados.Split(new[] { "+CTSDSR" }, StringSplitOptions.None);

        foreach (string item in _Messages)
        {
            if(Program.opcoes.DebugOut_PosData > 0)
                Console.Write("> AnalisaSerialRX: " + item);  
            
            if(!item.Contains(','))         // trap
                continue;

            string[] itemData = item.Split(',');

            if(itemData.Length != 6)        // trap
                continue;

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string Ssi = itemData[1];
            string Pdu = itemData[5].Split('\n')[1].Substring(2,19) + "0" ;//

            List<string> sPdu = pduDecoder.SLRPDUDecoder(Pdu, Ssi);

            Globals.PosDataReceived posDataReceived = new Globals.PosDataReceived
            {
                SSI = Ssi,
                Lat = double.TryParse(sPdu[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ? lat : double.NaN,
                Long = double.TryParse(sPdu[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng) ? lng : double.NaN,
                Timestamp = timestamp,
                Dir = double.TryParse(sPdu[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var dir) ? dir : double.NaN,
                Vel = double.TryParse(sPdu[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var vel) ? vel : double.NaN,
                Precision = Convert.ToInt32(sPdu[4]),
            };

            messageQueuing.Enviar(posDataReceived);
        }
        dados = string.Empty;
    }

    public void SerialPortTx(string texto)
    {
        if (serialport is { IsOpen: true })
            serialport.WriteLine(texto);
    }

     public void SerialPortFechar()
    {
        if (serialport is { IsOpen: true })
            serialport.Close();
    }

    public void Dispose()
    {
        SerialPortFechar();
        serialport?.Dispose();
    }

}
