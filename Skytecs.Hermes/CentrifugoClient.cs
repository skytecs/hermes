using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Skytecs.Hermes
{
    public class CentrifugoClient : IDisposable
    {
        private object _lock = new object();
        private const int _sendChunkSize = 256;
        private const int _receiveChunkSize = 256;
        private const bool _verbose = true;
        private readonly TimeSpan _delay = TimeSpan.FromMilliseconds(30000);
        private readonly IOptions<CentrifugoSettings> _settings;
        private readonly ILogger<CentrifugoClient> _logger;
        private Encoding _encoder = Encoding.UTF8;
        private ClientWebSocket _webSocket;

        public CentrifugoClient(ILogger<CentrifugoClient> logger, IOptions<CentrifugoSettings> settings)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task Connect()
        {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(_settings.Value.CentrifugoUrl), CancellationToken.None);

            var parameters = new Parameters
            {
                User = Guid.NewGuid().ToString(),
                Timestamp = ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString(),
                //Info = OPTIONAL JSON ENCODED STRING, 
            };

            parameters.Token = GetToken(parameters.User + parameters.Timestamp);

            var message = new Message
            {
                Uid = Guid.NewGuid().ToString(),
                Method = CentrifugoMethods.Connect,
                Parameters = parameters
            };

            await Send(_webSocket, JsonConvert.SerializeObject(message));
            await Receive(_webSocket);
        }

        public async Task Subscribe()
        {
            Message message = new Message
            {
                Uid = Guid.NewGuid().ToString(),
                Method = CentrifugoMethods.Subscribe,
                Parameters = new Parameters
                {
                    Channel = _settings.Value.CentrifugoChannel
                }
            };

            await Send(_webSocket, JsonConvert.SerializeObject(message));
            await Receive(_webSocket);
        }

        public async Task Unsubscribe(string channel)
        {
            Message message = new Message
            {
                Uid = Guid.NewGuid().ToString(),
                Method = CentrifugoMethods.Unsubscribe,
                Parameters = new Parameters
                {
                    Channel = channel
                }
            };

            await Send(_webSocket, JsonConvert.SerializeObject(message));
            await Receive(_webSocket);
        }

        public async Task Listen(Action<JObject> onMessageReceived)
        {
            _logger.Info("Listen");
            while (true)
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    byte[] buffer = new byte[_receiveChunkSize];
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    _logger.Info("Receive response");
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        _logger.Warn($"Close: {Enum.GetName(typeof(WebSocketCloseStatus), result.CloseStatus)}, {result.CloseStatusDescription}");
                    }
                    else
                    {
                        lock (_lock)
                        {
                            var data = _encoder.GetString(buffer);
                            _logger.Info($"Response\n{data}");
                            try
                            {
                                var response = JsonConvert.DeserializeObject<Response>(data);

                                onMessageReceived(response.Body.Data);
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e);
                            }
                        }
                    }
                }

                _logger.Info("Disconnceted! Try to reconnect...");

                try
                {
                    await Connect();
                    await Subscribe();

                    _logger.Info("Reconnect was successful.");
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            }
        }

        public async Task Publish(string channel, string data)
        {
            Message message = new Message
            {
                Uid = Guid.NewGuid().ToString(),
                Method = CentrifugoMethods.Publish,
                Parameters = new Parameters
                {
                    Channel = channel,
                    Data = data
                }
            };

            await Send(_webSocket, JsonConvert.SerializeObject(message));
            await Receive(_webSocket);
        }

        public async Task Presence(string channel)
        {
            Message message = new Message
            {
                Uid = Guid.NewGuid().ToString(),
                Method = CentrifugoMethods.Presence,
                Parameters = new Parameters
                {
                    Channel = channel,
                }
            };

            await Send(_webSocket, JsonConvert.SerializeObject(message));
            await Receive(_webSocket);
        }

        public async Task History(string channel)
        {
            Message message = new Message
            {
                Uid = Guid.NewGuid().ToString(),
                Method = CentrifugoMethods.History,
                Parameters = new Parameters
                {
                    Channel = channel,
                }
            };

            await Send(_webSocket, JsonConvert.SerializeObject(message));
            await Receive(_webSocket);
        }

        public async Task Ping()
        {
            Message message = new Message
            {
                Uid = Guid.NewGuid().ToString(),
                Method = CentrifugoMethods.Ping
            };

            await Send(_webSocket, JsonConvert.SerializeObject(message));
            await Receive(_webSocket);
        }




        private string GetToken(string data)
        {
            var hmac = new HMACSHA256(_encoder.GetBytes(_settings.Value.CentrifugoSecret));
            var hash = hmac.ComputeHash(_encoder.GetBytes(data));

            string hex = BitConverter.ToString(hash);
            return hex.Replace("-", "").ToLowerInvariant();
        }

        private async Task Send(ClientWebSocket webSocket, string message)
        {
            byte[] buffer = _encoder.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

            LogStatus(false, buffer, buffer.Length);
        }

        private async Task Receive(ClientWebSocket webSocket)
        {
            byte[] buffer = new byte[_receiveChunkSize];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                Console.WriteLine($"Close: {Enum.GetName(typeof(WebSocketCloseStatus), result.CloseStatus)}, {result.CloseStatusDescription}");
            }
            else
            {
                LogStatus(true, buffer, result.Count);
            }
        }

        private void LogStatus(bool receiving, byte[] buffer, int length)
        {

            lock (_lock)
            {
                //Console.ForegroundColor = receiving ? ConsoleColor.Green : ConsoleColor.Gray;
                //Console.WriteLine("{0} ", receiving ? "Received" : "Sent"); 

                //if (_verbose)
                string way = receiving ? "Received" : "Sent";
                _logger.Info($"{way}\n{_encoder.GetString(buffer)}\n");

                //Console.ResetColor();
            }

        }

        public void Dispose()
        {
            if (_webSocket != null)
            {
                _webSocket.Dispose();
            }
        }
    }
    /*{
   "method": "message",
   "body": {
      "uid": "GUDeKfLCVGMTq7UeQkkuWj",
      "channel": "test_ch",
      "data": {
         "method": "receipt",
         "parameters": null
      }
   }
}*/
    [Serializable]
    public class Response
    {
        [JsonProperty("method")]
        public string Method { get; set; }
        [JsonProperty("body")]
        public ResponseBody Body { get; set; }

        [Serializable]
        public class ResponseBody
        {
            [JsonProperty("uid")]
            public string Uid { get; set; }
            [JsonProperty("channel")]
            public string Channel { get; set; }
            [JsonProperty("data")]
            public JObject Data { get; set; }
        }
    }


    public static class CentrifugoMethods
    {
        public static string Connect { get { return "connect"; } }
        public static string Subscribe { get { return "subscribe"; } }
        public static string Unsubscribe { get { return "unsubscribe"; } }
        public static string Publish { get { return "publish"; } }
        public static string Presence { get { return "presence"; } }
        public static string History { get { return "history"; } }
        public static string Ping { get { return "ping"; } }
    }

    [Serializable]
    public class Message
    {
        [JsonProperty("uid")]
        public string Uid { get; set; }
        [JsonProperty("method")]
        public string Method { get; set; }
        [JsonProperty("params")]
        public Parameters Parameters { get; set; }

    }

    [Serializable]
    public class Parameters
    {
        [JsonProperty("user")]
        public string User { get; set; }
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("channel")]
        public string Channel { get; set; }
        [JsonProperty("data")]
        public string Data { get; set; }
    }

}
