using MMBot.HipChat;
using Xunit;

namespace MMBot.Tests
{
    public class WordParserTest
    {
        [Fact]
        public void Pop()
        {
            var words = new WordParser("one two three");
            Assert.Equal("one", words.Pop());
            Assert.Equal("two", words.Pop());
            Assert.Equal("three", words.Pop());
            Assert.Null(words.Pop());
        }

        [Fact]
        public void PopQuoteWord()
        {
            var words = new WordParser("one \"two bits\" three");
            Assert.Equal("one", words.Pop());
            Assert.Equal("two bits", words.Pop());
            Assert.Equal("three", words.Pop());
            Assert.Null(words.Pop());
        }
    }
}