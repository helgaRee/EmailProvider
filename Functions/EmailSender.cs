using Azure.Messaging.ServiceBus;
using EmailProvider.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmailProvider.Functions
{
	public class EmailSender(ILogger<EmailSender> logger, IEmailService emailService)
	{
		private readonly ILogger<EmailSender> _logger = logger;
		private readonly IEmailService _emailService = emailService;

		[Function(nameof(EmailSender))]
		public async Task Run(
			[ServiceBusTrigger("email_request", Connection = "ServiceBusConnection")]
			ServiceBusReceivedMessage message,
			ServiceBusMessageActions messageActions)
		{

			try
			{
				var emailRequest = _emailService.UnpackEmailRequest(message);
				if (emailRequest != null && !string.IsNullOrEmpty(emailRequest.To))
				{
					//bygger meddelandet
					if (_emailService.SendEmail(emailRequest))
					{
						await messageActions.CompleteMessageAsync(message);
					}

				}

			}
			catch (Exception ex)
			{
				_logger.LogError($"ERROR : EmailSender.Run :: {ex.Message}");
			}

		}

	}
}
