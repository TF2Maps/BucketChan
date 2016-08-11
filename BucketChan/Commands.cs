using System;
using System.Collections.Generic;

namespace BucketChan
{
    public class ChatCommands
    {
        private readonly Dictionary<string, IChatCommand> _commands = new Dictionary<string, IChatCommand>();

        public void Register(string pattern, IChatCommand command)
        {
            // Parse the pattern
            if (!pattern.StartsWith("!") || pattern.Contains(" "))
            {
                throw new Exception("Pattern in invalid format");
            }
            var commandName = pattern.TrimStart('!');

            // Add the command
            _commands[commandName] = command;
        }

        public void Handle(string msg, ChatResponder responder)
        {
            // Make sure we actually got passed a command
            if (!msg.StartsWith("!"))
            {
                throw new Exception("Handle must be passed a command");
            }

            // Take out the actual command parts
            var sections = msg.TrimStart('!').Split(' ');

            // Try to find the command
            IChatCommand command;
            if (sections.Length == 0 || !_commands.TryGetValue(sections[0], out command))
            {
                // If we didn't find a command, just ignore it, it's probably for dogbot or something
                return;
            }

            // Invoke the command
            command.Invoke(sections, responder);
        }
    }

    public interface IChatCommand
    {
        void Invoke(string[] input, ChatResponder responder);
    }

    public class FuncCommand : IChatCommand
    {
        private readonly Action<string[], ChatResponder> _command;

        public FuncCommand(Action<string[], ChatResponder> command)
        {
            _command = command;
        }

        public FuncCommand(Func<string[], string> command)
        {
            _command = (msg, responder) => responder.SendPublic(command(msg));
        }

        public void Invoke(string[] input, ChatResponder responder)
        {
            _command(input, responder);
        }
    }
}