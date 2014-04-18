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
            Assert.Equal("bitbucket", message.From);
        }

        [Fact]
        public void Notify()
        {
            HipchatMessage message = parser.Parse("::notify this message will notify");
            Assert.Equal("this message will notify", message.Contents);
            Assert.True(message.Notify);
        }

        [Fact]
        public void Html()
        {
            HipchatMessage message = parser.Parse("::html message is html");
            Assert.Equal("message is html", message.Contents);
            Assert.Equal("html", message.Format);
        }

        [Fact]
        public void Text()
        {
            HipchatMessage message = parser.Parse("::text message is text");
            Assert.Equal("message is text", message.Contents);
            Assert.Equal("text", message.Format);
        }

        [Fact]
        public void Red()
        {
            Assert.Equal("red", parser.Parse("::red wat").BackgroundColor);
        }

        [Fact]
        public void Yellow()
        {
            Assert.Equal("yellow", parser.Parse("::yellow wat").BackgroundColor);
        }

        [Fact]
        public void Green()
        {
            Assert.Equal("green", parser.Parse("::green wat").BackgroundColor);
        }

        [Fact]
        public void Purple()
        {
            Assert.Equal("purple", parser.Parse("::purple wat").BackgroundColor);
        }

        [Fact]
        public void Gray()
        {
            Assert.Equal("gray", parser.Parse("::gray wat").BackgroundColor);
        }

        [Fact]
        public void Random()
        {
            Assert.Equal("random", parser.Parse("::random wat").BackgroundColor);
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

        [Fact]
        public void Plain()
        {
            Assert.Equal("this is plain text", parser.Parse("this is plain text").Contents);
        }
    }
}