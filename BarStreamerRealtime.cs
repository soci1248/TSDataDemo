using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace TSDataDemo;

/// <summary>
/// Starts streaming market data from TradeStation and raises BarArrived events till stopped
/// </summary>
public sealed class BarStreamerRealtime(string host)
{
    /// <summary>
    /// If no heartbeat arrives within this interval we reconnect
    /// </summary>
    private const int NetworkTimeout = 1000 * 90;
    
    private Ticker _ticker;
    private TradeStationSteamingBar Bar { get; set; }
    private string StreamerName { get; set; }
    private int MaxRetryAtTimeout { get; } = int.MaxValue;
    private TaskCompletionSource<bool> TaskCompletionSourceForStreamingCompleted { get; } = new();

    private StreamWriter _writer;

    private static readonly TimeSpan NetworkTimeoutTimeSpan = TimeSpan.FromMilliseconds(NetworkTimeout);
    private Uri _resourceUri;
    private string _line;
    private bool _stopRequested;
    private int _timeoutCounter;

    private TaskCompletionSource<bool> _streamingStartedTaskCompletionSource;

    private readonly JsonSerializerSettings _microsoftDateFormatSettings = new()
    {
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
    };

    public Task StartAsync(Ticker requestedTicker, CancellationToken cancellationToken)
    {
        _ticker = requestedTicker;
        _writer = File.AppendText($"Log{requestedTicker.TickerTS}.txt");
        _writer.AutoFlush = true;

        return StartStreamingAsync(cancellationToken);
    }

    private Task StartStreamingAsync(CancellationToken cancellationToken)
    {
        _streamingStartedTaskCompletionSource = new TaskCompletionSource<bool>();

        _ = Task.Run((async () =>
        {
            Thread.CurrentThread.Name = StreamerName;
            while (!cancellationToken.IsCancellationRequested && !_stopRequested)
            {
                try
                {
                    //Streaming can be resumed from the last realtime reception
                    await using var responseStream = await SendStreamBarChartRequestAsync().ConfigureAwait(false);
                    using var streamReader = new StreamReader(responseStream, Encoding.UTF8, false, 1);

                    //This will be called multiple times because of the retry mechanism due to network problems.
                    //SetResult would throw on calling second time, hence we use TrySetResult
                    _streamingStartedTaskCompletionSource.TrySetResult(true);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        _line = await streamReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                        if (_line == null)
                        {
                            Log("Null returned from reading network data, probably the streaming ended. Reconnecting...");
                            break;
                        }
                        _timeoutCounter = 0;

                        Log(_line);

                        if (StartsWith("ERROR"))
                        {
                            Log("Some error happened, reconnecting...");

                            break;
                        }

                        if (StartsWith("{\"Heartbeat\":"))
                        {
                        }
                        else
                        {
                            try
                            {
                                Bar = JsonConvert.DeserializeObject<TradeStationSteamingBar>(_line, _microsoftDateFormatSettings);
                            }
                            catch (Exception ex)
                            {
                                Log($"Bogus line: {_line}, {ex}");
                            }

                            if (Bar.Open.IsZero() && Bar.High.IsZero() && Bar.Low.IsZero() && Bar.Close.IsZero())
                            {
                                Log($"Potentially bogus data. Bar: {Bar}");
                            }
                        }
                    }
                }
                catch (TaskCanceledException ex)
                {
                    if (ex.InnerException is TimeoutException)
                    {
                        if (_timeoutCounter++ > MaxRetryAtTimeout)
                        {
                            ExitWithError(new TimeoutException($"Timeout more than {MaxRetryAtTimeout} times, give up"));
                        }
                    }

                    Log(ex);
                    Log("Connection closed, retrying...");
                }
                catch (HttpRequestException ex)
                {
                    Log(ex);
                    Log("Connection closed, retrying...");
                }
                catch (IOException ex)
                {
                    Log(ex);
                    Log("Connection closed, retrying...");
                }
                catch (Exception ex)
                {
                    Log(ex);
                    throw;
                }
                Thread.Sleep(1000);
            }
        }), cancellationToken);

        return _streamingStartedTaskCompletionSource.Task;
    }

    private void Log(Exception exception) => Log(exception.ToString());

    private void Log(string message)
    {
        var f = $"{DateTime.Now} {StreamerName} {message}";
        
        Console.WriteLine(f);
        _writer.WriteLine(f);
    }

    private Task<Stream> SendStreamBarChartRequestAsync()
    {
        StreamerName = $"RTS {GetHashCode()} {_ticker.TickerTS}";

        if (string.IsNullOrEmpty(_ticker.TickerTS))
        {
            throw new InvalidOperationException("Ticker.TickerTS was null or empty. You should fill it for TradeStation api calls.");
        }

        _resourceUri = new Uri($"{host}/marketdata/stream/barcharts/{WebUtility.UrlEncode(_ticker.TickerTS)}" +
                               $"?interval=1" +
                               $"&unit=minute" + 
                               $"&barsback=1");

        Log($"Streaming Bars for {_ticker.TickerTS}");
        Log($"Request url: {_resourceUri} with token: {AccessToken.Instance.access_token}");

        HttpClient client = new()
        {
            Timeout = NetworkTimeoutTimeSpan,
            DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", AccessToken.Instance.access_token) }
        };
        Task<Stream> stream = client.GetStreamAsync(_resourceUri);

        return stream;
    }

    private void ExitWithError(Exception ex)
    {
        Log($"Fatal error in BarStreamer: {ex.Message}");
        TaskCompletionSourceForStreamingCompleted.TrySetException(ex);
        _stopRequested = true;
    }

    private bool StartsWith(string value)
    {
        return _line.StartsWith(value, StringComparison.Ordinal);
    }
}