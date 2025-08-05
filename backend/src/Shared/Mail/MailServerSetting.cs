using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Mail
{
	public class MailServerSetting
	{
		public string Host { get; set; }
		public int Port { get; set; }
		public bool UseSSL { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public string EmailTest { get; set; }
		public string SiteUrl { get; set; }
		public string DisplayName { get; set; }
		public string FromAddress { get; set; }
	}
}
