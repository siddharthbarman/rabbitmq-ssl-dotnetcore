using RabbitMQ.Client;
using System.Configuration;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;

namespace sender
{
    internal class Program
	{
		static void Help()
		{
			Console.WriteLine("Sample program to send message to RabbitMQ.");
			Console.WriteLine("Syntax:");
			Console.WriteLine("sender <queue> <message>");
		}

		static T GetSetting<T>(string key, T defaultValue)
		{
			string value = ConfigurationManager.AppSettings[key];
			if (string.IsNullOrEmpty(value)) 
			{
				return defaultValue;
			}
			else
			{
				return (T)System.Convert.ChangeType(value, typeof(T));
			}
		}

		static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				Help();
				return;
			}

			string server = GetSetting<string>("server", Environment.MachineName);
			int port = GetSetting<int>("port", 5672);
			bool useSSL = GetSetting<bool>("usessl", false);
			string user = GetSetting<string>("user", "");
			string password = GetSetting<string>("password", "");
            string clientCert = GetSetting<string>("clientcert", string.Empty);
			string clientCertPass = GetSetting<string>("clientcertpassword", string.Empty);

			Console.WriteLine($"server={server}");
            Console.WriteLine($"port={port}");
            Console.WriteLine($"usessl={useSSL}");
            Console.WriteLine($"user={user}");
            Console.WriteLine($"password={password}");
            Console.WriteLine($"clientcert={clientCert}");
            Console.WriteLine($"clientcertpassword={clientCertPass}");

            if (!string.IsNullOrEmpty(clientCert))
			{			
				if (!File.Exists(clientCert))
				{
					Console.WriteLine("The specified client certificate was not found!");
					return;
				}
			}
						
			string queueName = args[0];
			string message = args[1];
			
			SendMessage(server, port, useSSL, user, password, clientCert, clientCertPass, queueName, message);
			Console.WriteLine("Message sent.");
		}

		static void SendMessage(string server, int port, bool useSSL, string user, string password, string clientCert, string clientCertPass, 
			string queueName, string message)
		{
			using (IConnection conn = CreateConnection(server, port, useSSL, user, password, clientCert, clientCertPass))
			{
				using (IModel model = conn.CreateModel())
				{
					model.QueueDeclare(queueName, false, false, true, null);					
					IBasicProperties properties = model.CreateBasicProperties();
					byte[] messagebuffer = Encoding.Default.GetBytes(message);
					model.BasicPublish(string.Empty, queueName, properties, messagebuffer);
				}				
			}			
		}

		static IConnection CreateConnection(string server, int port, bool useSSL, string user, string password, string clientCert, string clientCertPass)
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			ConnectionFactory factory = new ConnectionFactory()
			{	
				Port = port,
				HostName = server,
				ClientProvidedName = "Sender",
				AutomaticRecoveryEnabled = true,
				NetworkRecoveryInterval = TimeSpan.FromSeconds(60),
				UserName = user,
				Password = password
			};

			factory.VirtualHost = "/";
			factory.Ssl.Version = SslProtocols.Tls12;
			factory.Ssl.Enabled = useSSL;
			factory.Ssl.ServerName = server;
			factory.Ssl.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNotAvailable;
			
            if (!string.IsNullOrEmpty(clientCert))
			{
				factory.Ssl.CertPath = clientCert;
				factory.AuthMechanisms = new List<RabbitMQ.Client.IAuthMechanismFactory> { new ExternalMechanismFactory() };
			}

			if (!string.IsNullOrEmpty(clientCertPass))
			{
				factory.Ssl.CertPassphrase = clientCertPass;
			}

			return factory.CreateConnection();
		}
    }
}