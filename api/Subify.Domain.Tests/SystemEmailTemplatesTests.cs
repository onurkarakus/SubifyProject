using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Domain.Tests;

public class SystemEmailTemplatesTests
{
    [Fact]
    public void All_has_exactly_three_templates_times_two_locales()
    {
        Assert.Equal(6, SystemEmailTemplates.All.Count);
        Assert.Equal(
            new[]
            {
                SystemEmailTemplates.Names.Invite,
                SystemEmailTemplates.Names.RenewalReminder,
                SystemEmailTemplates.Names.ResetPassword
            },
            SystemEmailTemplates.All.Select(t => t.Name).Distinct().OrderBy(n => n).ToArray());
    }

    [Fact]
    public void All_has_no_verify_email_template()
    {
        Assert.DoesNotContain(
            SystemEmailTemplates.All,
            t => t.Name.Equals(SystemEmailTemplates.Names.VerifyEmail, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Each_template_has_tr_and_en()
    {
        foreach (var name in SystemEmailTemplates.SeededNames)
        {
            var langs = SystemEmailTemplates.All
                .Where(t => t.Name == name)
                .Select(t => t.LanguageCode)
                .OrderBy(l => l)
                .ToArray();

            Assert.Equal(new[] { "en", "tr" }, langs);
        }
    }

    [Fact]
    public void Templates_contain_expected_placeholders()
    {
        var reset = SystemEmailTemplates.All.Single(t =>
            t.Name == SystemEmailTemplates.Names.ResetPassword && t.LanguageCode == "en");
        Assert.Contains("{{FullName}}", reset.Body);
        Assert.Contains("{{ResetUrl}}", reset.Body);

        var renewal = SystemEmailTemplates.All.Single(t =>
            t.Name == SystemEmailTemplates.Names.RenewalReminder && t.LanguageCode == "en");
        Assert.Contains("{{SubscriptionName}}", renewal.Body);
        Assert.Contains("{{Amount}}", renewal.Body);
        Assert.Contains("{{RenewalDate}}", renewal.Body);

        var invite = SystemEmailTemplates.All.Single(t =>
            t.Name == SystemEmailTemplates.Names.Invite && t.LanguageCode == "en");
        Assert.Contains("{{InviteUrl}}", invite.Body);
        Assert.Contains("{{InviterName}}", invite.Body);
        Assert.Contains("{{InstanceName}}", invite.Body);
    }

    [Fact]
    public void Create_normalizes_language_and_sets_id()
    {
        var template = EmailTemplates.Create("ResetPassword", "EN", "Subject", "<p>Body</p>");

        Assert.Equal("en", template.LanguageCode);
        Assert.Equal("ResetPassword", template.Name);
        Assert.NotEqual(Guid.Empty, template.Id);
    }

    [Fact]
    public void Subjects_fit_max_length_255()
    {
        Assert.All(SystemEmailTemplates.All, t => Assert.True(t.Subject.Length <= 255, t.Name));
    }
}
