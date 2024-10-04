using Azure.Messaging.ServiceBus;
using EmailProvider.Models;

namespace EmailProvider.Services;

//definierar 3 metoder
public interface IEmailService
{
	//skickar ett epostmeddelande baserat på en given EmailRequest
	bool SendEmail(EmailRequest emailRequest);

	//Packar upp ett Service Bus-meddelande till ett EmailRequest
	EmailRequest UnpackEmailRequest(ServiceBusReceivedMessage message);

	//Skapar och skickar ett bekräftelsemeddelande till användaren
	Task SendConfirmationEmail(string toEmail, string name);
}