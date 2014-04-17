using MMBot.HipChat;
using Xunit;

namespace MMBot.Tests
{
    public class HipchatMessageParserTests
    {
        readonly HipchatMessageParser parser = new HipchatMessageParser();

        [Fact]
        public void BackgroundColor()
        {
            HipchatMessage message = parser.Parse("::background yellow       And this is my message");
            Assert.Equal("And this is my message", message.Contents);
            Assert.Equal("yellow", message.BackgroundColor);
        }

        [Fact]
        public void Format()
        {
            HipchatMessage message = parser.Parse("::format html    And this is <b>bold</b>");
            Assert.Equal("And this is <b>bold</b>", message.Contents);
            Assert.Equal("html", message.Format);
        }

        [Fact]
        public void From()
        {
            HipchatMessage message = parser.Parse("::from bitbucket    Message from bitbucket");
            Assert.Equal("Message from bitbucket", message.Contents);
            Assert.Equal("bitbucket", message.From);
        }

        [Fact]
        public void MessageThatNotifies()
        {
            HipchatMessage message = parser.Parse("::notify this message will notify");
            Assert.Equal("this message will notify", message.Contents);
            Assert.True(message.Notify);
        }

        [Fact]
        public void Combined()
        {
            HipchatMessage message = parser.Parse("::background yellow ::format text ::from mmbot ::notify Message has all the things");
            Assert.Equal("yellow", message.BackgroundColor);
            Assert.Equal("text", message.Format);
            Assert.Equal("mmbot", message.From);
            Assert.True(message.Notify);
            Assert.Equal("Message has all the things", message.Contents);
        }
    }
}