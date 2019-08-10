using System;

namespace ActorWordsCounter.Writes
{
    public class ConsoleWriter : IWriteStuff
    {
        public void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}
