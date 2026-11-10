using System.Collections.Concurrent;
public class MessageDeQueuing
{
    private readonly ConcurrentQueue<Globals.PosDataReceived> _fila;
    public MessageDeQueuing(ConcurrentQueue<Globals.PosDataReceived> fila) => _fila = fila;

    public async Task ExecutarAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                while (_fila.TryDequeue(out var posD))
                {
                    Console.WriteLine($"DeQueing: {posD.SSI}");
                   // string resultado = await SendToTraccar.PostStringAsync(posD);
                    SendToTraccar.GetAsync(posD);
                }
                    
                await Task.Delay(100, ct);                   // polling
            }
        }
        catch (OperationCanceledException)
        {
            // oh no, cancelar ???!!!!!!
        }
     
        while (_fila.TryDequeue(out var resto))             // esvazia o que sobrou se cancelar
            Console.WriteLine($"DeQueing (resto): {resto}");
    }    

}