namespace TSDataDemo;

/// <summary>
/// Stream 1 ticker data from TradeStation, including some days of backfill data
/// </summary>
public class TradeStationSingleTickerRealtimeTickSource(TradeStationWebApi tradeStationWebApi)
{
    private Ticker _requestedTicker;
    private BarStreamerRealtime _barStreamer;

    public void Init(Ticker ticker)
    {
        _requestedTicker = ticker;
    }

    public Task StreamTickDataAsync(CancellationToken cancellationToken)
    {
        if (_requestedTicker == null)
        {
            throw new InvalidOperationException("Should call Init() first");
        }

        _barStreamer = tradeStationWebApi.CreateBarStreamerRealtime();

        return _barStreamer.StartAsync(_requestedTicker, cancellationToken);
    }
}