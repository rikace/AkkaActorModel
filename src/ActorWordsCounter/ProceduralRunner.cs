using ActorWordsCounter.Readers;
using ActorWordsCounter.Writes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ActorWordsCounter
{
    
  public class ProceduralRunner
    {
        public static void Run()
        {
            var writer = new ConsoleWriter();
            var reader = new ConsoleReader();

            writer.WriteLine("PROCEDURAL:::");

            var file = PrintInstructionsAndGetFile(reader, writer);
            if (file == null)
            {
                return;
            }

            var sw = new Stopwatch();
            sw.Start();

            var fileInfo = new FileInfo(file);
            var wordCounts = new Dictionary<String, Int32>();

            using (var fileReader = fileInfo.OpenText())
            {
                while (!fileReader.EndOfStream)
                {
                    var line = fileReader.ReadLine();
                    var cleanFileContents = Regex.Replace(line, @"[^\u0000-\u007F]", " ");

                    var wordArray = cleanFileContents.Split(new char[] { ' ' }, 
                        StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in wordArray)
                    {
                        if (wordCounts.ContainsKey(word))
                            wordCounts[word] += 1;
                        else
                            wordCounts.Add(word, 1);
                    }
                }
            }

            var topWords = wordCounts.OrderByDescending(w => w.Value).Take(25);
            foreach (var word in topWords)
            {
                writer.WriteLine($"{word.Key} == {word.Value} times");
            }

            sw.Stop();
            writer.WriteLine($"Elapsed time: {sw.ElapsedMilliseconds}");

            reader.ReadLine();
        }

        private static String PrintInstructionsAndGetFile(IReadStuff reader, IWriteStuff writer)
        {
            writer.WriteLine("Word counter.  Select the document to count:");
            writer.WriteLine(" (1) Magna Carta");
            writer.WriteLine(" (2) Declaration of Independence");
            var choice = reader.ReadLine();
            String file = AppDomain.CurrentDomain.BaseDirectory + @"\Files\";

            if (choice.Equals("1"))
            {
                file += @"MagnaCarta.txt";
            }
            else if (choice.Equals("2"))
            {
                file += @"DeclarationOfIndependence.txt";
            }
            else
            {
                writer.WriteLine("Invalid -- bye!");
                return null;
            }

            return file;
        }
    }
}
