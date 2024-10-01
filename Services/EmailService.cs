using Azure;
using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using EmailProvider.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace EmailProvider.Services;

public class EmailService(EmailClient emailClient, ILogger<EmailService> logger) : IEmailService
{
	private readonly EmailClient _emailClient = emailClient;
	private readonly ILogger<EmailService> _logger = logger;




	/// <summary>
	/// Tar emot ett ServiceBusMeddelande, avkodar det från JSON till ett Formsubmission-objekt
	/// och byggerdärefter ett EmailRequest som innehåller mottagarens epost, ämne och innehåll.
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	public EmailRequest UnpackEmailRequest(ServiceBusReceivedMessage message)
	{
		_logger.LogInformation($"Message Body: {message.Body.ToString()}");


		try
		{
			// Konvertera meddelandets kropp till en sträng
			string jsonMessage = Encoding.UTF8.GetString(message.Body.ToArray());

			// Deserialisera till FormSubmission-objekt
			var formSubmission = JsonConvert.DeserializeObject<FormSubmission>(jsonMessage);

			if (formSubmission != null)
			{
				// Skapa ett nytt EmailRequest-objekt
				return new EmailRequest
				{
					To = formSubmission.Email,
					Subject = "Bekräftelse på ditt kontaktformulär",
					HtmlBody = $"<p>Tack {formSubmission.Name} för att du kontaktade oss! Vi har mottagit ditt meddelande och kontaktar dig så snart vi kan.</p>",
					PlainText = $"Tack {formSubmission.Name} för att du kontaktade oss! Vi har mottagit ditt meddelande."
				};
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"ERROR : EmailSender.UnpackEmailRequest() :: {ex.Message}");
		}
		return null!;
	}




	/// <summary>
	/// Använder EmailClient för att skicka epost via Azure Communication Services.
	/// </summary>
	/// <param name="emailRequest"></param>
	/// <returns></returns>
	public bool SendEmail(EmailRequest emailRequest)
	{
		try
		{
			var result = _emailClient.Send(
				WaitUntil.Completed,
				senderAddress: Environment.GetEnvironmentVariable("SenderAddress"),
				recipientAddress: emailRequest.To,
				subject: emailRequest.Subject,
				htmlContent: emailRequest.HtmlBody,
				plainTextContent: emailRequest.PlainText);

			if (result.HasCompleted)
				return true;
		}
		catch (Exception ex)
		{
			_logger.LogError($"ERROR : EmailSender.SendEmailAsync() :: {ex.Message}");
		}

		return false;


	}







	/// <summary>
	/// Method that creates a confirm message for the specified email address in contact form
	/// Serialiserar meddelandet till JSON och skickar det till en Azure ServiceBus-kö
	/// </summary>
	/// <param name="toEmail"></param>
	/// <param name="name"></param>
	/// <returns>A confim message to user</returns>
	public async Task SendConfirmationEmail(string toEmail, string name)
	{


		var confirmationEmail = new EmailRequest
		{
			To = toEmail,
			Subject = "Bekräftelse på ditt kontaktformulär",
			HtmlBody = $"<p>Tack {name} för att du kontaktade oss! Vi har mottagit ditt meddelande och kontaktar dig så snart vi kan.</p>",
			PlainText = $"Tack {name} för att du kontaktade oss! Vi har mottagit ditt meddelande."
		};

		// Skicka e-post via EmailClient
		//var response = await _emailClient.SendAsync(
		//    WaitUntil.Completed,
		//    senderAddress: Environment.GetEnvironmentVariable("SenderAddress"),
		//    recipientAddress: confirmationEmail.To,
		//    subject: confirmationEmail.Subject,
		//    htmlContent: confirmationEmail.HtmlBody,
		//    plainTextContent: confirmationEmail.PlainText
		//);


		try
		{
			// Serialisera till JSON och skicka till Service Bus
			_logger.LogInformation($"toEmail: {toEmail}, name: {name}");

			string jsonMessage = JsonConvert.SerializeObject(confirmationEmail);
			ServiceBusMessage message = new ServiceBusMessage(jsonMessage);
			// Logga meddelandet som skickas
			_logger.LogInformation($"JSON Message to send: {jsonMessage}");



			// Skapa Service Bus-klient och skicka meddelandet
			var client = new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnection"));
			var sender = client.CreateSender("email_request");
			await sender.SendMessageAsync(message);

			// Logga att meddelandet har skickats till Service Bus
			_logger.LogInformation("Confirmation email request sent to Service Bus.");
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to send message to Service Bus: {ex.Message}");
		}
	}
}

