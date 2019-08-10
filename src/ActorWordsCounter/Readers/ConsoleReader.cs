using System;

namespace ActorWordsCounter.Readers
{
    public class ConsoleReader : IReadStuff
    {
        public string ReadLine()
        {
            return Console.ReadLine();
        }
    }
}
