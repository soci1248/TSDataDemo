namespace TSDataDemo;

//"High": "218.32",
//"Low": "212.42",
//"Open": "214.02",
//"Close": "216.39",
//"TimeStamp": "2020-11-04T21:00:00Z",
//"TotalVolume": "42311777",
//"DownTicks": 231021,
//"DownVolume": 19575455,
//"OpenInterest": "0",
//"IsRealtime": false,
//"IsEndOfHistory": false,
//"TotalTicks": 460552,
//"UnchangedTicks": 0,
//"UnchangedVolume": 0,
//"UpTicks": 229531,
//"UpVolume": 22736321,
//"Epoch": 1604523600000,
//"BarStatus": "Closed"
public class TradeStationSteamingBar
{
    public float High { get; set; }
    public float Low { get; set; }
    public float Open { get; set; }
    public float Close { get; set; }
    public DateTime TimeStamp { get; set; }
    public long TotalVolume { get; set; }
        
    public int DownTicks { get; set; }
    public long DownVolume { get; set; }
    public int OpenInterest { get; set; }
    public bool IsRealtime { get; set; }
    public bool IsEndOfHistory { get; set; }
    public int TotalTicks { get; set; }
    public int UnchangedTicks { get; set; }
    public int UnchangedVolume { get; set; }
    public int UpTicks { get; set; }
    public long UpVolume { get; set; }
    public string BarStatus { get; set; }

    public override string ToString()
    {
        return $"{nameof(TimeStamp)}: {TimeStamp}, {nameof(Open)}: {Open}, {nameof(High)}: {High}, {nameof(Low)}: {Low}, {nameof(Close)}: {Close}, {nameof(TotalVolume)}: {TotalVolume}, {nameof(IsRealtime)}: {IsRealtime}, {nameof(DownTicks)}: {DownTicks}, {nameof(DownVolume)}: {DownVolume}, {nameof(OpenInterest)}: {OpenInterest}, {nameof(TotalTicks)}: {TotalTicks}, {nameof(UnchangedTicks)}: {UnchangedTicks}, {nameof(UnchangedVolume)}: {UnchangedVolume}, {nameof(UpTicks)}: {UpTicks}, {nameof(UpVolume)}: {UpVolume}";
    }
}