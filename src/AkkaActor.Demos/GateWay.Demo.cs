namespace AkkaActor.Demos
{
   
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class WebsiteStressTestMessage
    {
        private readonly Uri _url;
        public Uri Url { get { return _url; } }

        public WebsiteStressTestMessage(Uri url)
        {
            _url = url;
        }
    }

    class LoadTestingActor : UntypedActor
    {
        private readonly HttpClient _httpClient;
        public LoadTestingActor()
        {
            _httpClient = new HttpClient();
        }
        protected override void OnReceive(object message)
        {
            if(message is WebsiteStressTestMessage)
            {
                var loadTest = (WebsiteStressTestMessage)message;
                Console.WriteLine("Load testing site {0}", loadTest.Url.ToString());
                _httpClient.GetStringAsync(loadTest.Url).PipeTo(Self);
            }
            else if(message is string)
            {
                Console.WriteLine("Successfully downloaded web page");
            }
        }
    }

    public class SendSms
    {
        private readonly string _phoneNumber;
        private readonly string _message;

        public string PhoneNumber { get { return _phoneNumber; } }
        public string Message { get { return _message; } }
        public SendSms(string phoneNumber, string message)
        {
            _phoneNumber = phoneNumber;
            _message = message;
        }
    }

    class SmsGatewayActor : ReceiveActor
    {

        public SmsGatewayActor()
        {
            Receive<SendSms>(sms =>
            {
                Console.WriteLine("Contacting SMS Gateway from actor {0}. Sending '{1}' to {2}", Self.Path, sms.Message, sms.PhoneNumber);
            });
        }
    }

  public static class TestLoadSms
    {
        public static void Run()
        {
            var configFile = File.ReadAllText("Scaling.conf");
            var config = ConfigurationFactory.ParseString(configFile);
            var actorSystem = ActorSystem.Create("MyActorSystem", config);

            var props =
                Props.Create<LoadTestingActor>()
                     .WithRouter(FromConfig.Instance);

            var loadTestingPool =
                actorSystem.ActorOf(props, "LoadGenerator");

            var smsGatewayProps =
                Props.Create<SmsGatewayActor>()
                    .WithRouter(new RoundRobinPool(10, new DefaultResizer(5, 10)));

            var smsGatewayPool =
                actorSystem.ActorOf(smsGatewayProps);

            loadTestingPool.Tell(new WebsiteStressTestMessage(new Uri("http://www.google.com")));

            smsGatewayPool.Tell(new SendSms("555-555-1234", "Hello world"));
            smsGatewayPool.Tell(new SendSms("555-555-1235", "Hello other world"));

            Console.ReadLine();
        }
    }
}
