using System.Net.Http.Json;
using System.Text;
using TetraPosTraccar;
public static class SendToTraccar
{   
    private static string ConvertToStr(Globals.PosDataReceived ld)
    {
        string r = string.Empty;
        r += "/";
        r += "?id=";
        r += ld.SSI;   
        r += "&lat="; 
        r += ld.Lat;
        r += "&lon=";
        r += ld.Long;
        r += "&timestamp=";
        r += ld.Timestamp;

        return r.Replace(',','.');
    }

    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri(Program.opcoes.Servidor),      // Reutilizando uma única instância p/ evitar esgotamento de sockets
        Timeout = TimeSpan.FromSeconds(15)
    };

    public static async Task<string> GetAsync(Globals.PosDataReceived locationData)
    {   
        string c = ConvertToStr(locationData);

        using var resp = await _http.GetAsync(c);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }

    public static async Task<string> PostStringAsync( Globals.PosDataReceived locationData,
        string mediaType = "text/plain")
    {
        string t = ConvertToStr(locationData);
        string caminho = Program.opcoes.Servidor;
        using var conteudo = new StringContent(t, Encoding.UTF8, mediaType);
        using var resp = await _http.PostAsync(caminho, conteudo);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }

    public static async Task<string> PostJsonAsync<T>(string caminho, T dados)
    {
        using var resp = await _http.PostAsJsonAsync(caminho, dados);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }
}