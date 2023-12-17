using RabbitMQ.Client;
using System.Configuration;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;

namespace receiver
{
    internal class Program
	{
		static void Help()
		{
			Console.WriteLine("Demo receiver program for RabbitMQ");
			Console.WriteLine("receiver <queue>");
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
			if (args.Length != 1)
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

            string queueName = args[0];

            ReadMessage(server, port, useSSL, user, password, clientCert, clientCertPass, queueName);
		}

		static void ReadMessage(string server, int port, bool useSSL, string user, string password, string clientCert, string clientCertPass,
            string queueName)
		{
			using (IConnection conn = CreateConnection(server, port, useSSL, user, password, clientCert, clientCertPass))
			{
				using (IModel model = conn.CreateModel())
				{
					ReadOnlyMemory<byte> messageBytes = model.BasicGet(queueName, true).Body;
					byte[] bytes = messageBytes.ToArray();
					string message = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);

					Console.WriteLine(message);					
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
                ClientProvidedName = "Receiver",
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