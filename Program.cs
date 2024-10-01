using Azure.Communication.Email;
using EmailProvider.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

//skapa och konfigurera en Azure Function-app som kommer k�ra tj�nsten EmailService
var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices(services =>
	{
		services.AddApplicationInsightsTelemetryWorkerService();
		services.ConfigureFunctionsApplicationInsights();
		//kommunicerar med Azure com. services f�r att skicka epost
		services.AddSingleton<EmailClient>(new EmailClient(Environment.GetEnvironmentVariable("CommunicationServices")));
		//egen tj�nst som hanerar epost-logik, beroende av EmailClient
		services.AddSingleton<IEmailService, EmailService>();
	})
	.Build();

host.Run();
