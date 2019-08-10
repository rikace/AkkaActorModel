namespace ActorWordsCounter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    
    
    public class Complete
    {
    }
    
    public class MappedList
    {
        public readonly Dictionary<String, Int32> LineWordCount;
        public readonly Int32 LineNumber;

        public MappedList(Int32 lineNumber, Dictionary<String, Int32> wordCounts)
        {
            LineNumber = lineNumber; LineWordCount = wordCounts;
        }
    }
    
    public class ReadLineForCounting
    {
        public readonly String Line;
        public readonly Int32 LineNumber;

        public ReadLineForCounting(Int32 lineNumber, String line)
        {
            LineNumber = lineNumber;
            Line = line;
        }
    }
    
    public class StartCount
    {
        public readonly String FileName;

        public StartCount(String file) { FileName = file; }
    }
}
