namespace MMBot.HipChat
{
    public class HipchatMessageParser
    {
        public HipchatMessage Parse(string raw)
        {
            raw = raw.Trim();

            if (!raw.StartsWith("::"))
            {
                return new HipchatMessage {Contents = raw, Format = "text"};
            }

            HipchatMessage message = new HipchatMessage();

            WordParser parser = new WordParser(raw);

            while (true)
            {
                if (parser.NextWordMatch("::background"))
                {
                    parser.Pop();
                    message.BackgroundColor = parser.Pop();
                }
                else if (parser.NextWordMatch("::format"))
                {
                    parser.Pop();
                    message.Format = parser.Pop();
                }
                else if (parser.NextWordMatch("::html"))
                {
                    parser.Pop();
                    message.Format = "html";
                }
                else if (parser.NextWordMatch("::text"))
                {
                    parser.Pop();
                    message.Format = "text";
                }
                else if (parser.NextWordMatch("::from"))
                {
                    parser.Pop();
                    message.From = parser.Pop();
                }
                else if (parser.NextWordMatch("::notify"))
                {
                    parser.Pop();
                    message.Notify = true;
                }
                else if (parser.NextWordMatch("::yellow", "::red", "::green", "::purple", "::gray", "::random"))
                {
                    var value = parser.Pop();
                    message.BackgroundColor = value.Substring(2);
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