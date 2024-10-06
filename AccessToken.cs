namespace TSDataDemo;

public sealed class AccessToken
{
    public static AccessToken Instance { get; } = new();

    private static readonly object ForLock = new();

    private string _accessToken;
    private string _expires_in;
    private string _refresh_token;
    private string _token_type;
    private string _userid;

    //The serializer force me to use public ctor
    public AccessToken()
    {
    }

    /// <summary>
    /// This is needed for every request
    /// </summary>
    public string access_token
    {
        get
        {
            lock (ForLock)
            {
                return _accessToken;
            }
        }
        set
        {
            lock (ForLock)
            {
                _accessToken = value;
            }
        }
    }

    public string expires_in
    {
        get
        {
            lock (ForLock)
            {
                return _expires_in;
            }
        }
        set
        {
            lock (ForLock)
            {
                _expires_in = value;
            }
        }
    }

    public string refresh_token
    {
        get
        {
            lock (ForLock)
            {
                return _refresh_token;
            }
        }
        set
        {
            lock (ForLock)
            {
                _refresh_token = value;
            }
        }
    }

    public string token_type
    {
        get
        {
            lock (ForLock)
            {
                return _token_type;
            }
        }
        set
        {
            lock (ForLock)
            {
                _token_type = value;
            }
        }
    }

    public string userid
    {
        get
        {
            lock (ForLock)
            {
                return _userid;
            }
        }
        set
        {
            lock (ForLock)
            {
                _userid = value;
            }
        }
    }

    public static void UpdateFrom(AccessToken token)
    {
        lock (ForLock)
        {
            Instance.access_token = token.access_token;
            Instance.expires_in = token.expires_in;
            Instance.refresh_token = token.refresh_token;
            Instance.token_type = token.token_type;
            Instance.userid = token.userid;
        }
    }
}