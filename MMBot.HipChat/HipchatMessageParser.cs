using System;

namespace MMBot.HipChat
{
    public class HipchatMessageParser
    {
        public HipchatMessage Parse(string raw)
        {
            HipchatMessage message = new HipchatMessage();

            WordParser parser = new WordParser(raw);

            while (true)
            {
                if (parser.Peek().Equals("::background", StringComparison.OrdinalIgnoreCase))
                {
                    parser.Pop();
                    message.BackgroundColor = parser.Pop();
                }
                else if (parser.Peek().Equals("::format", StringComparison.OrdinalIgnoreCase))
                {
                    parser.Pop();
                    message.Format = parser.Pop();
                }
                else if (parser.Peek().Equals("::from", StringComparison.OrdinalIgnoreCase))
                {
                    parser.Pop();
                    message.From = parser.Pop();
                }
                else if (parser.Peek().Equals("::notify", StringComparison.OrdinalIgnoreCase))
                {
                    parser.Pop();
                    message.Notify = true;
                }
                else
                {
                    break;
                }
            }

            message.Contents = parser.Remainder;

            return message;
        }
    }
}