using System.Net;
using System.Net.Mail;

namespace QanoonCoalition.Web.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config) => _config = config;

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var smtp = _config.GetSection("Smtp");
        var host     = smtp["Host"]     ?? "smtp.zoho.com";
        var port     = int.Parse(smtp["Port"] ?? "587");
        var fromAddr = smtp["From"]     ?? "no_reply@gcc.iq";
        var fromName = smtp["FromName"] ?? "ائتلاف دولة القانون";
        var user     = smtp["User"]     ?? fromAddr;
        var pass     = smtp["Password"] ?? "";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        var msg = new MailMessage
        {
            From       = new MailAddress(fromAddr, fromName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true
        };
        msg.To.Add(toEmail);

        await client.SendMailAsync(msg);
    }
}
