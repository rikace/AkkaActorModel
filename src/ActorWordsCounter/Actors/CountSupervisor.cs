using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using System.Diagnostics;
using ActorWordsCounter;
using ActorWordsCounter.Writes;

namespace ActorMapReduceWordCount.Actors
{
    public class CountSupervisor : ReceiveActor
    {
        public static Props Create(IWriteStuff writer, Stopwatch sw)
        {
            return Props.Create(() => new CountSupervisor(writer, sw));
        }

        private readonly IWriteStuff _writer;
        private Dictionary<String, Int32> _wordCount;
        private readonly Int32 _numberOfRoutees;
        private Int32 _completeRoutees;
        private Stopwatch _stopwatch;

        public CountSupervisor(IWriteStuff writer, Stopwatch sw)
        {
            _writer = writer;
            _wordCount = new Dictionary<String, Int32>();
            _numberOfRoutees = 5;
            _completeRoutees = 0;
            _stopwatch = sw;

            SetupBehaviors(writer);
        }

        private void SetupBehaviors(IWriteStuff writer)
        {
            Receive<StartCount>(msg =>
            {
                var fileInfo = new FileInfo(msg.FileName);
                var lineNumber = 0;

                var lineReader = Context.ActorOf(new RoundRobinPool(_numberOfRoutees)
                                    .Props(LineReaderActor.Create(writer)));

             

                using (var reader = fileInfo.OpenText())
                {
                    while (!reader.EndOfStream)
                    {
                        lineNumber++;

                        var line = reader.ReadLine();
                        lineReader.Tell(new ReadLineForCounting(lineNumber, line));
                    }
                }

                lineReader.Tell(new Broadcast(new Complete()));
            });

            Receive<MappedList>(msg =>
            {
                foreach (var key in msg.LineWordCount.Keys)
                {
                    if (_wordCount.ContainsKey(key))
                    {
                        _wordCount[key] += msg.LineWordCount[key];
                    }
                    else
                    {
                        _wordCount.Add(key, msg.LineWordCount[key]);
                    }
                }
            });

            Receive<Complete>(msg =>
            {
                _completeRoutees++;

                if (_completeRoutees == _numberOfRoutees)
                {
                    var topWords = _wordCount.OrderByDescending(w => w.Value).Take(25);
                    foreach (var word in topWords)
                    {
                        _writer.WriteLine($"{word.Key} == {word.Value} times");
                    }

                    _stopwatch.Stop();
                    writer.WriteLine($"Elapsed time: {_stopwatch.ElapsedMilliseconds}");
                }
            });
        }
    }
}
