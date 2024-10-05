namespace TSDataDemo;

internal class Program
{
    static void Main(string[] args)
    {
        var api = new TradeStationWebApi("XXXX",
            "XXXX", "http://localhost:1234/", TradeStationEnvironment.Live);

        var tickers = new string[] {
            "ESZ24","EMDZ24","NQZ24","YMZ24","MESZ24",
            "GCZ24","HGZ24","PLZ24",
            "CLX24","HOZ24","RBZ24", "NGX24",
            "FCX24","LHV24",
            "USZ24","TYZ24",
            "SX24", "SMZ24", "WZ24", "KWZ24",
            "FDAXZ24", "FSMIZ24", "FGBLZ24"
        };

        foreach (var tickerName in tickers)
        {
            var streamer = new TradeStationSingleTickerRealtimeTickSource(api);
            streamer.Init(new Ticker(tickerName));
            _ = streamer.StreamTickDataAsync(CancellationToken.None);

        }
        Console.ReadLine();
    }
}