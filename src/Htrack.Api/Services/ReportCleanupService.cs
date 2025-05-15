namespace HTrack.Api.Services;

public class ReportCleanupService(IWebHostEnvironment env)
{
    public void CleanupOldReports()
    {
        var reportDir = Path.Combine(env.ContentRootPath, "Reports");
        if (!Directory.Exists(reportDir)) return;

        var now = DateTime.UtcNow;
        foreach (var file in Directory.GetFiles(reportDir, "*.xlsx"))
        {
            var creationTime = File.GetCreationTimeUtc(file);
            if ((now - creationTime).TotalDays >= 1)
            {
                File.Delete(file);
            }
        }
    }
}