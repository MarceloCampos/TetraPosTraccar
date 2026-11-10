using System.Threading.Channels;
using System.Collections.Concurrent;
using TetraPosTraccar;

public class MessageQueuing
{
    private readonly ConcurrentQueue<Globals.PosDataReceived> _fila;
    public MessageQueuing(ConcurrentQueue<Globals.PosDataReceived> fila) => _fila = fila;

    public void Enviar(Globals.PosDataReceived dado) => _fila.Enqueue(dado);
}
