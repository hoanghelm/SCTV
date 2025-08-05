using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Mail
{
	public interface IMailService
	{
		Task<bool> SendAsync(MailMessage mailMessage, bool keepReceiver = false);
	}
}
