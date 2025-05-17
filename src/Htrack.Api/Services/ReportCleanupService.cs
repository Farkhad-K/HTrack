namespace HTrack.Api.Services;

public class ReportCleanupService(
    IWebHostEnvironment env,
    ILogger<ReportCleanupService> logger)
{
    public void CleanupOldReports()
    {
        // var reportDir = Path.Combine(env.ContentRootPath, "Reports");
        // if (!Directory.Exists(reportDir))
        // {
        //     logger.LogWarning("Reports directory not found: {ReportDir}", reportDir);
        //     return;
        // }

        // var files = Directory.GetFiles(reportDir, "*.xlsx");
        // logger.LogInformation("Deleting all {FileCount} report files.", files.Length);

        // foreach (var file in files)
        // {
        //     try
        //     {
        //         File.Delete(file);
        //         logger.LogInformation("Deleted file: {FilePath}", file);
        //     }
        //     catch (Exception ex)
        //     {
        //         logger.LogError(ex, "Error deleting file: {FilePath}", file);
        //     }
        // }

        var reportDir = Path.Combine(env.ContentRootPath, "Reports");
        if (Directory.Exists(reportDir))
        {
            Directory.Delete(reportDir, recursive: true);
            logger.LogInformation("Deleted entire Reports folder.");
        }

        Directory.CreateDirectory(reportDir);
        logger.LogInformation("Re‑created empty Reports folder.");
    }
}