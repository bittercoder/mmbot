﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.iq.roster;
using agsXMPP.protocol.x.muc;
using agsXMPP.Xml.Dom;
using Common.Logging;
using Newtonsoft.Json;
using Uri = System.Uri;

namespace MMBot.HipChat
{
    public class HipChatAdapter : Adapter
    {
        private static string _host;
        private static string[] _rooms;
        private static string[] _logRooms;
        private static string _nick;
        private static string _password;
        private static string _authToken;
        private static bool _isConfigured = false;
        private static Uri _apiMessageRoomUrl;
        private XmppClientConnection _client = null;
        private string _username;
        private string _confhost;
        private string _roomNick;
        private readonly Dictionary<string, string> _roster = new Dictionary<string, string>();
        HipchatMessageParser parser = new HipchatMessageParser();

        public HipChatAdapter(ILog logger, string adapterId)
            : base(logger, adapterId)
        {
        }

        public override void Initialize(Robot robot)
        {
            base.Initialize(robot);
            Configure();
        }

        private void Configure()
        {
            _authToken = Robot.GetConfigVariable("MMBOT_HIPCHAT_AUTHTOKEN");
            _apiMessageRoomUrl  =new Uri(string.Format(@"https://api.hipchat.com/v1/rooms/message?format=json&auth_token={0}", Uri.EscapeDataString(_authToken)));
            _host = Robot.GetConfigVariable("MMBOT_HIPCHAT_HOST") ?? "chat.hipchat.com";
            _confhost = Robot.GetConfigVariable("MMBOT_HIPCHAT_CONFHOST") ?? "conf.hipchat.com";
            _nick = Robot.GetConfigVariable("MMBOT_HIPCHAT_NICK");
            _roomNick = Robot.GetConfigVariable("MMBOT_HIPCHAT_ROOMNICK");
            _username = Robot.GetConfigVariable("MMBOT_HIPCHAT_USERNAME");
            _password = Robot.GetConfigVariable("MMBOT_HIPCHAT_PASSWORD");
            _rooms = (Robot.GetConfigVariable("MMBOT_HIPCHAT_ROOMS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            _logRooms = (Robot.GetConfigVariable("MMBOT_HIPCHAT_LOGROOMS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            if (_host == null || _nick == null | _password == null || !_rooms.Any())
            {
                var helpSb = new StringBuilder();
                helpSb.AppendLine("The HipCat adapter is not configured correctly and hence will not be enabled.");
                helpSb.AppendLine("To configure the HipChat adapter, please set the following configuration properties:");
                helpSb.AppendLine("  MMBOT_HIPCHAT_HOST: The host name defaults to chat.hipchat.com");
                helpSb.AppendLine("  MMBOT_HIPCHAT_CONFHOST: The host name defaults to conf.hipchat.com");
                helpSb.AppendLine("  MMBOT_HIPCHAT_NICK: The nick name of the bot account on HipChat, e.g. mmbot");
                helpSb.AppendLine("  MMBOT_HIPCHAT_ROOMNICK: The name of the bot account on HipChat, e.g. mmbot Bot");
                helpSb.AppendLine("  MMBOT_HIPCHAT_USERNAME: The username of the bot account on HipChat, e.g. 70126_494074");
                helpSb.AppendLine("  MMBOT_HIPCHAT_PASSWORD: The password of the bot account on HipChat");
                helpSb.AppendLine("  MMBOT_HIPCHAT_ROOMS: A comma separated list of room names that mmbot should join");
                helpSb.AppendLine("  MMBOT_HIPCHAT_AUTHTOKEN: An optional authorization token (for v1 API) that if set will allow room messages to use hip-chat specific formatting features.");
                helpSb.AppendLine("More info on these values and how to create the mmbot.ini file can be found at https://github.com/mmbot/mmbot/wiki/Configuring-mmbot");
                Logger.Warn(helpSb.ToString());
                _isConfigured = false;
            }
            else
            {
                _isConfigured = true;
            }
        }

        public override async Task Run()
        {
            if (!_isConfigured) {
                throw new AdapterNotConfiguredException();
            }
            Logger.Info(string.Format("Logging into HipChat..."));

            SetupHipChatClient();
        }

        private void SetupHipChatClient()
        {
            if (_client != null) {
                return;
            }
            
            _client = new XmppClientConnection(_host);
            _client.AutoResolveConnectServer = false;
            _client.OnLogin += OnClientLogin;
            _client.OnMessage += OnClientMessage;
            _client.OnError += OnClientError;
            _client.OnAuthError += OnClientAuthError;
            _client.Resource = "bot";
            _client.UseStartTLS = true;

            Logger.Info(string.Format("Connecting to {0}", _host));
            _client.Open(_username, _password);
            Logger.Info(string.Format("Connected to {0}", _host));

            _client.OnRosterStart += OnClientRosterStart;
            _client.OnRosterItem += OnClientRosterItem;
        }

        private void OnClientAuthError(object sender, Element e)
        {
            Logger.Error("Error authenticating in HipChat client");
        }

        private void OnClientError(object sender, Exception ex)
        {
            Logger.Error("Error in HipChat client", ex);
        }

        private void OnClientMessage(object sender, agsXMPP.protocol.client.Message message)
        {
            if (!String.IsNullOrEmpty(message.Body))
            {
                Console.WriteLine("Message : {0} - from {1}", message.Body, message.From);

                string user;

                if (message.Type != MessageType.groupchat)
                {
                    if (!_roster.TryGetValue(message.From.User, out user))
                    {
                        user = "Unknown User";
                    }
                }
                else
                {
                    user = message.From.Resource;
                }

                if (user == _roomNick)
                    return;

                
                Logger.Info(string.Format("[{0}] {1}: {2}", DateTime.Now, user, message.Body.Trim()));

                var userObj = Robot.GetUser(message.Id, user, message.From.Bare, Id);

                if (userObj.Name != _nick)
                {
                    Task.Run(() =>
                        Robot.Receive(new TextMessage(userObj, message.Body.Trim())));
                }
            }
        }

        public override async Task Send(Envelope envelope, params string[] messages)
        {
            await base.Send(envelope, messages);

            if (messages == null || !messages.Any()) return;

            foreach (var message in messages)
            {
                var to = new Jid(envelope.User.Room);
                if (MustUseXMPP(to))
                {
                    _client.Send(new agsXMPP.protocol.client.Message(to, string.Equals(to.Server, _confhost) ? MessageType.groupchat : MessageType.chat, RemoveFormatSpecifiers(message)));
                }
                else if (!string.IsNullOrWhiteSpace(message))
                {
                    var hipchatMessage = parser.Parse(message);
                    await PostMessageToRoomUsingAPI(hipchatMessage, envelope.User.Room);
                }
            }
        }

        public override async Task Reply(Envelope envelope, params string[] messages)
        {
            await base.Reply(envelope, messages);

            if (messages == null || !messages.Any()) return;

            var userAddress = _roster.Where(kvp => kvp.Value == envelope.User.Name).Select(kvp => kvp.Key).First() + '@' + _host;
            foreach (var message in messages)
            {
                var to = new Jid(userAddress);
                _client.Send(new agsXMPP.protocol.client.Message(to, string.Equals(to.Server, _confhost) ? MessageType.groupchat : MessageType.chat, RemoveFormatSpecifiers(message)));
            }
        }

        public override async Task Emote(Envelope envelope, params string[] messages)
        {
            await base.Emote(envelope, messages);

            if (messages == null || !messages.Any()) return;

            foreach (var message in messages.Select(m => "/me " + m))
            {
                var to = new Jid(envelope.User.Room);
                _client.Send(new agsXMPP.protocol.client.Message(to, string.Equals(to.Server, _confhost) ? MessageType.groupchat : MessageType.chat, RemoveFormatSpecifiers(message)));
            }
        }

        private void OnClientLogin(object sender)
        {
            var mucManager = new MucManager(_client);

            foreach (string room in _rooms.Union(_logRooms).Distinct())
            {
                var jid = new Jid(room + "@" + _confhost);
                mucManager.JoinRoom(jid, _roomNick);
                Rooms.Add(room);
                Logger.Info(string.Format("Joined Room '{0}'", room));
            }
            foreach (string logRoom in _logRooms)
            {
                LogRooms.Add(logRoom);
            }
        }

        private void OnClientRosterItem(object sender, RosterItem item)
        {
            if (!_roster.ContainsKey(item.Jid.User))
            {
                _roster.Add(item.Jid.User, item.Name);
                Logger.Info(string.Format("User '{0}' logged in", item.Name));
            }
        }

        private void OnClientRosterStart(object sender)
        {

        }

        public override async Task Topic(Envelope envelope, params string[] messages)
        {
            if(envelope != null && envelope.User != null)
            {
                await Topic(envelope.User.Room, messages);
            }
        }

        public override async Task Topic(string roomName, params string[] messages)
        {
            var mucManager = new MucManager(_client);
            mucManager.ChangeSubject(new Jid(roomName), string.Join(" ", messages));
        }
        

        public override async Task Close()
        {
            _client.Close();
            _client = null;
        }
        
        private async Task PostMessageToRoomUsingAPI(HipchatMessage message, string roomId)
        {
            var client = new HttpClient();

            roomId = ExtractHipchatRoomIdOnly(roomId);

            var parameters = new Dictionary<string, string>
            {
                {"room_id", roomId},
                {"notify", message.Notify ? "1" : "0"},
                {"message_format", message.Format ?? "text"},
                {"message", message.Contents},
                {"from", message.From ?? _nick}
            };

            if (message.BackgroundColor != null)
            {
                parameters["color"] = message.BackgroundColor;
            }
            
            var content = new FormUrlEncodedContent(parameters);

            var response = await client.PostAsync(_apiMessageRoomUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception(string.Format("Failed to post message to room - message was:\r\n\r\n{0}\r\n\r\nresponse was:\r\n\r\n{1}", JsonConvert.SerializeObject(parameters), body));
            }
        }

        string ExtractHipchatRoomIdOnly(string roomId)
        {
            if (roomId == null) throw new ArgumentException("roomId");
            return roomId.Contains("_") ? roomId.Split('@')[0].Split('_')[1] : roomId;
        }

        string RemoveFormatSpecifiers(string message)
        {
            return new HipchatMessageParser().Parse(message).Contents;
        }

        bool MustUseXMPP(Jid to)
        {
            return _authToken == null || !string.Equals(to.Server, _confhost);
        }

    }
}
