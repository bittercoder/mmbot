using System;
using System.Linq;

namespace MMBot.HipChat
{
    public static class WordParserExtensions
    {
        public static bool NextWordMatch(this WordParser parser, params string[] commands)
        {
            var next = parser.Peek();
            if (next == null) return false;
            return commands.Any(cmd => cmd.Equals(next, StringComparison.OrdinalIgnoreCase));
        }
    }
}