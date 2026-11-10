/*
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.Binder
dotnet add package System.IO.Ports
   dotnet restore

protocolo OsmAnd com timestamp Unix epoch em segundos:
http://garoacloud.com.br:5055/?id=123456&lat=-23.5995&lon=-46.9999&timestamp=1730700000   

Serial no Linux: o usuário precisa estar no grupo dialout, senão o Open() dá UnauthorizedAccessException:
     sudo usermod -aG dialout $USER   # depois fazer logout/login
*/
using Microsoft.Extensions.Configuration;

namespace TetraPosTraccar;



internal class Program
{
    public static AppSettings? opcoes;
    static bool SendInitSerialTest = false;
    
    static void Main(string[] args)
    {
        Console.WriteLine("> Init TetraPosTraccar...");

        if (OperatingSystem.IsLinux())
        {
            Console.WriteLine("> Detectado Linux");   

        }
        else if (OperatingSystem.IsWindows())
            Console.WriteLine("> Detectado Windows");

        Console.WriteLine("> Arq. Options: " + "appsettings.json");
        
        GetConfig();

        SerialRadio serialRadio = new SerialRadio();

        serialRadio.SerialPortConfig();

        MessageDeQueuing messageDeQueuing = new MessageDeQueuing(serialRadio.filaMessages);
        
        using var cts = new CancellationTokenSource();
        var tarefa = messageDeQueuing.ExecutarAsync(cts.Token);

        Console.WriteLine("> Init Concluido");

        while(true)
        {
            Thread.Sleep(20);
            //serialRadio.SerialPortTx("The Quick BrownFox Jumps over the Lazy Dog\r\n");          
        }
    }

    static void GetConfig()
    {

        var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

        opcoes = config.GetSection("TetraPosTraccar").Get<AppSettings>()!;

        Console.WriteLine("> Servidor: " + opcoes.Servidor);
        Console.WriteLine("> Serial: " + (OperatingSystem.IsLinux() ? opcoes.SerialLinux : opcoes.SerialWindows) );

        int intervalo = config.GetValue<int>("Traccar:IntervaloSegundos", 60);  // unico
        
    }

}

