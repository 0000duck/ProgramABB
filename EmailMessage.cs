using System.Net.Mail;
using System.Net;

namespace ProgramABB
{
    class EmailMessage
    {
        public EmailMessage()
        {
            smtpClient = new SmtpClient();
            smtpClient.UseDefaultCredentials = false;
            smtpClient.EnableSsl = true;
            smtpClient.Port = 587;
            smtpClient.Host = smtpHost;
            smtpClient.Credentials = new NetworkCredential(address, password);

            message = new MailMessage();
            MailAddress from = new MailAddress(address, userName);
            message.From = from;
            message.To.Add(address);
        }

        public void SetSubject(string subject)
        {
            message.Subject = subject;
        }

        public void SetBody(string body)
        {
            message.Body = body;
        }

        public bool Send()
        {
            try
            {
                smtpClient.SendAsync(message, address);
            }
            catch (SmtpException)
            {
                return false;
            }

            return true;
        }

        private const string address = "rafalcz29@gmail.com";
        private const string userName = "Rafał Czajka";
        private const string password = "Haslo123";
        private const string smtpHost = "smtp.gmail.com";

        private SmtpClient smtpClient = null;
        private MailMessage message = null;
    }
}
