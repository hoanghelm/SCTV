using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Mail
{
	public class MailMessage
	{
		public string DisplayName { get; set; }
		public string From { get; set; }
		public string To { get; set; }
		public string Cc { get; set; }
		public string Bcc { get; set; }
		public string Subject { get; set; }
		public string PlainTextMessage { get; set; }
		public string HtmlMessage { get; set; }
		public string ExtensionString { get; set; }
	}
}
