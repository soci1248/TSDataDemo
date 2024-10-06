using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace TSDataDemo;

public class TradeStationWebApi
{
    private readonly Timer _timer;
    private string Key { get; }
    private string Secret { get; }
    private string Host { get; }
    private string HostV2 { get; set; }
    private string HostV3 { get; set; }

    private string RedirectUri { get; }
    private readonly TimeSpan _refreshTokenPeriod = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _refreshTokenPeriodForFailedAttempt = TimeSpan.FromMinutes(2);
    private readonly object _forLock = new();

    private StreamWriter _writer;

    public TradeStationWebApi([NotNull] string key, [NotNull] string secret, [NotNull] string redirectUri, TradeStationEnvironment environment)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Secret = secret ?? throw new ArgumentNullException(nameof(secret));
        RedirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));

        ServicePointManager.DefaultConnectionLimit = 1000;

        _writer = File.AppendText("LogApi.txt");
        _writer.AutoFlush = true;

        // ReSharper disable once AsyncVoidLambda
        _timer = new Timer(async _ =>
        {
            await RefreshAccessToken().ConfigureAwait(false);
        }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        switch (environment)
        {
            case TradeStationEnvironment.Simulation:
                HostV3 = "https://sim.api.tradestation.com/v3";
                HostV2 = "https://sim.api.tradestation.com/v2";
                Host = "https://sim.api.tradestation.com/v2";
                break;
            case TradeStationEnvironment.Live:
                HostV3 = "https://api.tradestation.com/v3";
                HostV2 = "https://api.tradestation.com/v2";
                Host = "https://api.tradestation.com/v2";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(environment), environment, null);
        }

        Log("Reading cached token");
        var token = TryReadCachedToken();

        if (token == null)
        {
            Log("No cached token found, getting a new one from TS");
            token = AsyncContext.Run(() => GetAccessToken(GetAuthorizationCode()));
            SaveToken(token);

            Log("Updating local token");
            AccessToken.UpdateFrom(token);
        }
        else
        {
            Log("Refreshing token");
            bool success = AsyncContext.Run(RefreshAccessToken);
            if (!success)
            {
                token = AsyncContext.Run(() => GetAccessToken(GetAuthorizationCode()));
                SaveToken(token);
                Log("Updating local token");
                AccessToken.UpdateFrom(token);
            }
        }
    }

    private static AccessToken TryReadCachedToken()
    {
        string setting = new SettingsStore().GetSetting();
        if (!string.IsNullOrEmpty(setting))
        {
            var token = JsonConvert.DeserializeObject<AccessToken>(setting);
            if (token != null)
            {
                token.access_token = null;
                token.expires_in = null;
                return token;
            }
        }

        return null;
    }

    private static void SaveToken(AccessToken token)
    {
        new SettingsStore().SaveSetting(JsonConvert.SerializeObject(token));
    }

    private string GetAuthorizationCode()
    {
        var uri = $"https://signin.tradestation.com/authorize?response_type=code&client_id={Key}&redirect_uri={RedirectUri}&audience=https://api.tradestation.com&scope=MarketData&state=Valami";
        Process.Start(new ProcessStartInfo(uri)
        {
            UseShellExecute = true,
            Verb = "open"
        });

        using var listener = new HttpListener();
        listener.Prefixes.Add(RedirectUri);
        listener.Start();

        var context = listener.GetContext();
        var req = context.Request;
        var res = context.Response;

        var responseString = "<html><body><script>window.open('','_self').close();</script></body></html>";
        var buffer = Encoding.UTF8.GetBytes(responseString);
        res.ContentLength64 = buffer.Length;
        var output = res.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();

        listener.Stop();
        return req.QueryString.Get("code");
    }

    private async Task<AccessToken> GetAccessToken(string authcode)
    {
        var postData = $"grant_type=authorization_code&code={authcode}&client_id={Key}&redirect_uri={RedirectUri}&client_secret={Secret}";

        using var httpClient = new HttpClient();
        var content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await httpClient.PostAsync($"https://signin.tradestation.com/oauth/token", content).ConfigureAwait(false);

        // Handle response
        if (response.IsSuccessStatusCode)
        {
            // Read and process response
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Log(responseBody);

            try
            {
                return GetDeserializedResponse<AccessToken>(responseBody);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                Console.ReadLine();
                Environment.Exit(-1);
                throw;
            }
        }

        Log($"Failed to make request. Status code: {response.StatusCode}");

        return null;
    }

    private static T GetDeserializedResponse<T>(string jsonResponse)
    {
        var scrubbedJson = jsonResponse.Replace("\"__type\":\"EquitiesOptionsOrderConfirmation:#TradeStation.Web.Services.DataContracts\",", ""); // hack
        return JsonConvert.DeserializeObject<T>(scrubbedJson);
    }

    private async Task<bool> RefreshAccessToken()
    {
        using var client = new HttpClient();
        var url = $"https://signin.tradestation.com/oauth/token";
        var content = new FormUrlEncodedContent(
        [
            KeyValuePair.Create("grant_type", "refresh_token"),
            KeyValuePair.Create("client_id", Key),
            KeyValuePair.Create("client_secret", Secret),
            KeyValuePair.Create("refresh_token", AccessToken.Instance.refresh_token),
        ]);

        try
        {
            Log($"Getting refresh token: {url}");
            var response = await client.PostAsync(url, content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Log(responseContent);
            var newToken = GetDeserializedResponse<AccessToken>(responseContent);
            lock (_forLock)
            {
                Log("Updating local token");
                AccessToken.UpdateFrom(newToken);
            }

            _timer.Change(_refreshTokenPeriod, Timeout.InfiniteTimeSpan);
            return true;
        }
        catch (Exception ex)
        {
            Log(ex.Message);
            _timer.Change(_refreshTokenPeriodForFailedAttempt, Timeout.InfiniteTimeSpan);
            return false;
        }
    }

    public BarStreamerRealtime CreateBarStreamerRealtime()
    {
        return new BarStreamerRealtime(HostV3);
    }

    private void Log(string message)
    {
        var f = $"{DateTime.Now} {message}";

        Console.WriteLine(f);
        _writer.WriteLine(f);
    }

}