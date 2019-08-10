namespace Shared
{
    public class TestMessage
    {
        public string Message { get; private set; }
        public TestMessage(string message)
        {
            this.Message = message;
        }
    }
}
