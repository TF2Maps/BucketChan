using System;
using System.Collections.Generic;
using System.Linq;

namespace BucketChan
{
    class Program
    {
        static void Main(string[] args)
        {
            var auth = new AuthDetails(args[0], args[1]);

            var commands = new ChatCommands();
            RegisterMapTracking(commands);

            var bot = new ChatBot(auth, commands);
            bot.RunConnection();
        }

        static void RegisterMapTracking(ChatCommands commands)
        {
            var maps = new List<Tuple<string, string>>();

            commands.Register("!maps", new FuncCommand((msg, responder) =>
            {
                responder.SendPublic("Maps: " + string.Join(", ", maps.Select(v => v.Item1)));

                responder.SendPrivate("This formatted message is for gameday hosting convenience.");
                responder.SendPrivate(string.Join(" | ", maps.Select(v => v.Item1 + " : " + v.Item2)));
            }));

            var addCommand = new FuncCommand(msg =>
            {
                if (msg.Length != 3)
                {
                    return "Invalid format: !add <map> <url>";
                }

                maps.Add(Tuple.Create(msg[1], msg[2]));
                return "Added " + msg[1] + "";
            });

            commands.Register("!add", addCommand);
            commands.Register("!addmap", addCommand);
        }
    }
}