using System;

namespace MMBot.HipChat
{
    public class WordParser
    {
        static readonly Tuple<string, int> Exhausted = Tuple.Create((string) null, Int32.MaxValue);

        readonly string _text;
        int _position;

        public WordParser(string text)
        {
            _text = text;
        }

        public string Remainder
        {
            get { return _position < _text.Length - 1 ? _text.Substring(_position) : null; }
        }

        public string Peek()
        {
            Tuple<string, int> result = GetNext();
            return result.Item1;
        }

        Tuple<string, int> GetNext()
        {
            int start = _position;

            if (IsExhausted(start)) return Exhausted;

            start = ReadPastWhitespace(start);

            if (IsExhausted(start)) return Exhausted;

            if (IsCurrentQuote(start)) return ParseQuoted(start);

            return ParseUnquoted(start);
        }

        int ReadPastWhitespace(int start)
        {
            while (char.IsWhiteSpace(_text[start]) && start < _text.Length)
            {
                start++;
            }
            return start;
        }

        bool IsExhausted(int start)
        {
            return start > _text.Length - 1;
        }

        Tuple<string, int> ParseUnquoted(int start)
        {
            int cur = start;

            while (true)
            {
                if (char.IsWhiteSpace(_text[cur]))
                {
                    string word = _text.Substring(start, cur - start);
                    do
                    {
                        cur++;
                    } while (char.IsWhiteSpace(_text[cur]));

                    if (start == _text.Length - 1)
                    {
                        return Exhausted;
                    }

                    return Tuple.Create(word, cur);
                }

                if (cur < _text.Length - 1)
                {
                    cur++;
                }
                else
                {
                    break;
                }
            }

            return Tuple.Create(_text.Substring(start), Int32.MaxValue);
        }

        Tuple<string, int> ParseQuoted(int start)
        {
            start++;
            int cur = start;

            do
            {
                cur++;
            } while ((cur < _text.Length) && _text[cur] != '"' && _text[cur - 1] != '\\');

            string word = _text.Substring(start, cur - start);

            while (char.IsWhiteSpace(_text[cur]) && cur < _text.Length)
            {
                cur++;
            }

            return Tuple.Create(word, cur + 1);
        }

        bool IsCurrentQuote(int cur)
        {
            return _text[cur] == '"';
        }

        public string Pop()
        {
            Tuple<string, int> result = GetNext();
            _position = result.Item2;
            return result.Item1;
        }
    }
}