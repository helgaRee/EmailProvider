using Azure.Messaging.ServiceBus;
using EmailProvider.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmailProvider.Functions
{
	/// <summary>
	/// En azure function som lyssnar till en Service bus-kö (email_request)
	/// Ansvarar för att genomföra E-postutskicken
	/// </summary>
	/// <param name="logger"></param>
	/// <param name="emailService"></param>
	public class EmailSender(ILogger<EmailSender> logger, IEmailService emailService)
	{
		private readonly ILogger<EmailSender> _logger = logger;
		private readonly IEmailService _emailService = emailService;


		/// <summary>
		/// Tar emot meddelanden från Service Bus.
		/// Använder IEmailService för att packa upp medd. till en Email_request
		/// om korrekt epost-medd. skickas det via SendEmail
		/// markeras som slutfört i Service bus om lyckat epostutskick.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="messageActions"></param>
		/// <returns></returns>
		[Function(nameof(EmailSender))]
		public async Task Run(
			[ServiceBusTrigger("email_request", Connection = "ServiceBusConnection")]
			ServiceBusReceivedMessage message,
			ServiceBusMessageActions messageActions)
		{

			// Logga mottaget meddelande
			_logger.LogInformation($"Received message with ID: {message.MessageId} and Body: {message.Body.ToString()}");


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
