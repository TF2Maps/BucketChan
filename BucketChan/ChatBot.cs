using System;
using System.Threading;
using SteamKit2;

namespace BucketChan
{
    public class ChatBot
    {
        SteamClient _steamClient;
        CallbackManager _manager;

        SteamUser _steamUser;
        SteamFriends _steamFriends;

        private readonly AuthDetails _auth;
        private readonly ChatCommands _chatCommands;
        private bool _connected;
        private DateTime _lastActivity;

        public ChatBot(AuthDetails auth, ChatCommands chatCommands)
        {
            _auth = auth;
            _chatCommands = chatCommands;

            // create our steamclient instance
            _steamClient = new SteamClient();
            // create the callback manager which will route callbacks to function calls
            _manager = new CallbackManager(_steamClient);

            // get the steamuser handler, which is used for logging on after successfully connecting
            _steamUser = _steamClient.GetHandler<SteamUser>();
            _steamFriends = _steamClient.GetHandler<SteamFriends>();

            // register a few callbacks we're interested in
            // these are registered upon creation to a callback manager, which will then route the callbacks
            // to the functions specified
            _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            _manager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);
            _manager.Subscribe<SteamFriends.ChatMsgCallback>(OnChatMsg);

            _manager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKey);
        }

        public void RunConnection()
        {
            Console.WriteLine("Starting connection loop");

            while (true)
            {
                // Initiate the connection
                Console.WriteLine("Connecting to Steam...");
                _steamClient.Connect();

                // Create our callback handling loop
                _connected = true;
                _lastActivity = DateTime.Now;
                while (_connected)
                {
                    // in order for the callbacks to get routed, they need to be handled by the manager
                    _manager.RunWaitCallbacks(TimeSpan.FromSeconds(0.5f));

                    // Check how long it has been since activity
                    var elapsed = DateTime.Now - _lastActivity;
                    if (elapsed > TimeSpan.FromMinutes(5))
                    {
                        // It has been too long, rejoin
                        _connected = false;
                    }
                }

                // If we got here, we got disconnected in a way we didn't want to, so we need to retry connecting
                // According to the steamkit docs we should wait a bit before doing that
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine($"Unable to connect to Steam: {callback.Result}");

                _connected = false;
                return;
            }

            Console.WriteLine($"Connected to Steam! Logging in '{_auth.Username}'...");

            _steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = _auth.Username,
                Password = _auth.Password,
            });
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("Disconnected from Steam");

            _connected = false;
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                if (callback.Result == EResult.AccountLogonDenied)
                {
                    // if we recieve AccountLogonDenied or one of it's flavors (AccountLogonDeniedNoMailSent, etc)
                    // then the account we're logging into is SteamGuard protected
                    // see sample 5 for how SteamGuard can be handled

                    Console.WriteLine("Unable to logon to Steam: This account is SteamGuard protected.");

                    _connected = false;
                    return;
                }

                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

                _connected = false;
                return;
            }

            Console.WriteLine("Successfully logged on!");

            // at this point, we'd be able to perform actions on Steam

            // for this sample we'll just log off
            //steamUser.LogOff();
        }

        private void OnLoginKey(SteamUser.LoginKeyCallback callback)
        {
            Console.WriteLine("Attempting to join chat...");
            _steamFriends.JoinChat(new SteamID(103582791429594873));
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }

        private void OnAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            Console.WriteLine("Received account info");
            // before being able to interact with friends, you must wait for the account info callback
            // this callback is posted shortly after a successful logon

            // at this point, we can go online on friends, so lets do that
            _steamFriends.SetPersonaState(EPersonaState.Online);
        }

        private void OnChatMsg(SteamFriends.ChatMsgCallback callback)
        {
            _lastActivity = DateTime.Now;
            Console.WriteLine(callback.ChatterID + ": " + callback.Message);

            // If it's actually a command, pass it to the command parser
            var msg = callback.Message;
            if (msg.StartsWith("!"))
            {
                var responder = new ChatResponder
                {
                    PublicResponder = v =>
                        _steamFriends.SendChatRoomMessage(
                            callback.ChatRoomID,
                            EChatEntryType.ChatMsg,
                            v
                        ),
                    PrivateResponder = v =>
                        _steamFriends.SendChatMessage(
                            callback.ChatterID,
                            EChatEntryType.ChatMsg,
                            v
                        )
                };
                _chatCommands.Handle(msg, responder);
            }
        }
    }
}