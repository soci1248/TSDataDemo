namespace TSDataDemo;

internal class SettingsStore
{
    private const string FileName = "settings.json";

    public string GetSetting()
    {
        if (File.Exists(FileName))
        {
            return File.ReadAllText(FileName);
        }

        return null;
    }

    public void SaveSetting(string token)
    {
        File.WriteAllText(FileName, token);
    }
}