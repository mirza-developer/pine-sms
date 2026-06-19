namespace PineAI.Backup;

public class BackupSettings
{
    public string DatabaseName { get; set; } = string.Empty;
    public string LocalTempPath { get; set; } = Path.GetTempPath();
    public int IntervalHours { get; set; } = 1;
    public FtpSettings Ftp { get; set; } = new();
}

public class FtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 21;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RemotePath { get; set; } = "/";
}
