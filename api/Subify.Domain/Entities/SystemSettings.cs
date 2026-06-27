namespace Subify.Domain.Entities;

public class SystemSettings
{
    public string? AIApiKey { get; private set; }
    public string? SmtpHost { get; private set; }
    public int? SmtpPort { get; private set; }
    public string? SmtpUser { get; private set; }
    public string? SmtpPassword { get; private set; }
    public string? SmtpFromName { get; private set; }
    public string? SmtpFromEmail { get; private set; }

    protected SystemSettings() { }

    public void CreateSmtpSettings(string? smtpHost, int? smtpPort, string? smtpUser, string? smtpPassword, string? smtpFromName, string? smtpFromEmail)
    {
        SmtpHost = smtpHost;
        SmtpPort = smtpPort;
        SmtpUser = smtpUser;
        SmtpPassword = smtpPassword;
        SmtpFromName = smtpFromName;
        SmtpFromEmail = smtpFromEmail;
    }

    public void UpdateSmtpSettings(string? smtpHost, int? smtpPort, string? smtpUser, string? smtpPassword, string? smtpFromName, string? smtpFromEmail)
    {
        SmtpHost = smtpHost;
        SmtpPort = smtpPort;
        SmtpUser = smtpUser;
        SmtpPassword = smtpPassword;
        SmtpFromName = smtpFromName;
        SmtpFromEmail = smtpFromEmail;
    }

    public void CreateAIApiKey(string aIApiKey)
    {
        AIApiKey = aIApiKey;
    }

    public void UpdateAIApiKey(string aIApiKey)
    {
        AIApiKey = aIApiKey;
    }
}