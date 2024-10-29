using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using TicketApplication.Models;

namespace TicketApplication.Service
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SendRegistrationConfirmationMail(string recip, string customerName)
        {
            string message = string.Format(@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Registration Confirmation - TicketApp</title>
        </head>
        <body>
            <h2>Welcome to TicketApp, {0}!</h2>
            <p>Your registration has been successfully completed.</p>
            <p>Thank you for joining us, and enjoy booking tickets for your favorite events.</p>
            <p>Best Regards,</p>
            <p><strong>TicketApp Team</strong></p>
        </body>
        </html>", customerName);

            try
            {
                CheckEmailValid(recip);
                SendMail("Registration Confirmation - TicketApp", recip, message);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not send registration email. " + ex.Message);
            }
        }

        public void SendTicketOrderConfirmationMail(string recip, string customerName, Order order)
        {
            var ticketDetails = new StringBuilder();

            foreach (var detail in order.OrderDetails)
            {
                var eventTitle = detail.Ticket.Zone.Event.Title;
                var zoneName = detail.Ticket.Zone.Name;
                var price = detail.Ticket.Zone.Price;
                var eventDate = detail.Ticket.Zone.Event.Date;

                ticketDetails.AppendFormat(@"
            <div style='border: 2px solid #333; padding: 10px; margin: 10px 0;'>
                <h3>{0}</h3>
                <p><strong>Zone:</strong> {1}</p>
                <p><strong>Price:</strong> {2:C}</p>
                <p><strong>Date:</strong> {3}</p>
            </div>", eventTitle, zoneName, price, eventDate);
            }

            string message = string.Format(@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Order Confirmation - TicketApp</title>
        </head>
        <body>
            <h2>Your Ticket Order is Confirmed, {0}!</h2>
            <p>Thank you for your purchase. Here are the details of your order:</p>
            <div>
                {1}
            </div>
            <p>Total Amount: <strong>{2:C}</strong></p>
            <p>We look forward to seeing you at the event!</p>
            <p>Best Regards,</p>
            <p><strong>TicketApp Team</strong></p>
        </body>
        </html>", customerName, ticketDetails.ToString(), order.TotalAmount);

            try
            {
                CheckEmailValid(recip);
                SendMail("Order Confirmation - TicketApp", recip, message);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not send order confirmation email. " + ex.Message);
            }
        }


        public void SendTicketConfirmationMail(string recip, string customerName, string eventTitle, string zoneName, decimal price)
        {
            string message = string.Format("<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    " +
                    "<meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    " +
                    "<title>Xác nhận mua vé - TicketApp</title>\r\n</head>\r\n<body>\r\n    <h2>Vé của bạn đã được xác nhận!</h2>\r\n    " +
                    "<p>Xin chào <strong>{0},</strong></p>\r\n    " +
                    "<p>Bạn đã đặt vé thành công cho sự kiện: <strong>{1}</strong></p>\r\n    " +
                    "<p>Khu vực: <strong>{2}</strong></p>\r\n    " +
                    "<p>Giá vé: <strong>{3:C}</strong></p>\r\n    " +
                    "<p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>\r\n    " +
                    "<p>Trân trọng,</p>\r\n    <p style=\"font-style: italic; font-weight: bold;\">Đội ngũ TicketApp</p>\r\n</body>\r\n</html>\r\n",
                    customerName, eventTitle, zoneName, price);

            try
            {
                CheckEmailValid(recip);
                SendMail("Xác nhận mua vé - TicketApp", recip, message);
            }
            catch (Exception ex)
            {
                throw new Exception("Không thể gửi email. " + ex.Message);
            }
        }

        public void SendEventReminderMail(string recip, string customerName, string eventTitle, string eventDate, string zoneName)
        {
            string message = string.Format("<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    " +
                    "<meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    " +
                    "<title>Nhắc nhở sự kiện - TicketApp</title>\r\n</head>\r\n<body>\r\n    <h2>Nhắc nhở sự kiện của bạn!</h2>\r\n    " +
                    "<p>Xin chào <strong>{0},</strong></p>\r\n    " +
                    "<p>Sự kiện <strong>{1}</strong> mà bạn đã đặt vé sẽ diễn ra vào ngày: <strong>{2}</strong></p>\r\n    " +
                    "<p>Khu vực của bạn: <strong>{3}</strong></p>\r\n    " +
                    "<p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>\r\n    <p>Trân trọng,</p>\r\n    " +
                    "<p style=\"font-style: italic; font-weight: bold;\">Đội ngũ TicketApp</p>\r\n</body>\r\n</html>\r\n",
                    customerName, eventTitle, eventDate, zoneName);

            try
            {
                CheckEmailValid(recip);
                SendMail("Nhắc nhở sự kiện - TicketApp", recip, message);
            }
            catch (Exception ex)
            {
                throw new Exception("Không thể gửi email. " + ex.Message);
            }
        }

        public async void SendMail(string title, string recip, string body)
        {
            try
            {
                string fromMail = _configuration["EmailSetting:EmailID"];
                string fromPassword = _configuration["EmailSetting:AppPassword"];
                MailMessage message = new MailMessage
                {
                    From = new MailAddress(fromMail),
                    Subject = title,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(recip));

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(fromMail, fromPassword),
                    EnableSsl = true,
                };

                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                throw new Exception("Không thể gửi email: " + ex.Message);
            }
        }

        public void CheckEmailValid(string email)
        {
            string pattern = @"^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[a-zA-Z]{2,7}$";
            if (!Regex.IsMatch(email, pattern))
            {
                throw new Exception("Email không hợp lệ!");
            }

            string[] domain = email.Split('@');
            if (domain.Length < 2)
            {
                throw new Exception("Email không hợp lệ!");
            }

            IPHostEntry emailEntry = Dns.GetHostEntry(domain[1]);
            if (emailEntry == null || emailEntry.AddressList.Length == 0)
            {
                throw new Exception("Email không hợp lệ!");
            }
        }
    }
}
