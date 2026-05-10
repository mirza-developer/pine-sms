using System.IO.Compression;
using FluentFTP;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace PineSms.Backup;

public class Worker(ILogger<Worker> logger, IOptions<BackupSettings> options, IConfiguration configuration) : BackgroundService
{
    private readonly BackupSettings _settings = options.Value;
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Backup worker started. Interval: {Hours} hour(s).", _settings.IntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            try
            {
                await RunBackupCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the backup cycle.");
            }

            await Task.Delay(TimeSpan.FromHours(_settings.IntervalHours), stoppingToken);
        }
    }

    private async Task RunBackupCycleAsync(CancellationToken cancellationToken)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var bakFileName = $"{_settings.DatabaseName}_{timestamp}.bak";
        var zipFileName = $"{_settings.DatabaseName}_{timestamp}.zip";

        Directory.CreateDirectory(_settings.LocalTempPath);

        var bakFilePath = Path.Combine(_settings.LocalTempPath, bakFileName);
        var zipFilePath = Path.Combine(_settings.LocalTempPath, zipFileName);

        try
        {
            logger.LogInformation("Starting database backup: {Database}", _settings.DatabaseName);
            await BackupDatabaseAsync(bakFilePath, cancellationToken);
            logger.LogInformation("Database backup completed: {File}", bakFilePath);

            logger.LogInformation("Compressing backup file...");
            ZipBackupFile(bakFilePath, zipFilePath);
            logger.LogInformation("Compressed to: {File}", zipFilePath);

            logger.LogInformation("Uploading to FTP: {Host}", _settings.Ftp.Host);
            await UploadToFtpAsync(zipFilePath, zipFileName, cancellationToken);
            logger.LogInformation("Upload complete: {File}", zipFileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the backup cycle.");
        }
        finally
        {
            TryDeleteFile(bakFilePath);

            TryDeleteFile(zipFilePath);
        }
    }

    private async Task BackupDatabaseAsync(string bakFilePath, CancellationToken cancellationToken)
    {
        var sql = $"BACKUP DATABASE [{_settings.DatabaseName}] TO DISK = @path WITH FORMAT, INIT, COMPRESSION";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout = 3600
        };
        command.Parameters.AddWithValue("@path", bakFilePath);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void ZipBackupFile(string bakFilePath, string zipFilePath)
    {
        using var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
        zip.CreateEntryFromFile(bakFilePath, Path.GetFileName(bakFilePath), CompressionLevel.Optimal);
    }

    private async Task UploadToFtpAsync(string localFilePath, string remoteFileName, CancellationToken cancellationToken)
    {
        var config = new FtpConfig
        {
            EncryptionMode = FtpEncryptionMode.None,
            ValidateAnyCertificate = true,
            DataConnectionType = FtpDataConnectionType.PASV,
        };

        using var ftp = new AsyncFtpClient(_settings.Ftp.Host, _settings.Ftp.Username, _settings.Ftp.Password, _settings.Ftp.Port, config);
        await ftp.Connect(cancellationToken);

        var remotePath = $"{_settings.Ftp.RemotePath.TrimEnd('/')}/{remoteFileName}";
        await ftp.UploadFile(localFilePath, remotePath, FtpRemoteExists.Overwrite, true, FtpVerify.None, token: cancellationToken);

        await ftp.Disconnect(cancellationToken);
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete temp file: {Path}", path);
        }
    }
}
