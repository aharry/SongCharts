using System.Collections.Specialized;
using System.IO;
using System.Linq.Expressions;
using System.Xml;
using System.CommandLine;
using System.Text;

namespace SongCharts
{
    internal class Program
    {
        static List<BarBeat> BeatTimings = new();

        static async Task<int> Main(string[] args)
        {

            var inFile = new Option<string>(
                aliases: new string[] { "--inFile", "-if" },
                description: "The Song Master song file to process.");

            var outFile = new Option<string>(
                aliases: new string[] { "--outFile", "-of" },
                description: "The Song Master song file to process.");

            var startBar = new Option<int>(
                    aliases: new string[] { "--startBar", "-sb" },
                    description: "The bar to begin processing",
                    getDefaultValue: () => 0);

            var barBreak = new Option<int>(
                    aliases: new string[] { "--lineBreak", "-lb" },
                    description: "Number of bars per line.",
                    getDefaultValue: () => 4);
            
            var repeat = new Option<bool>(
                    aliases: new string[] { "--repeat", "-r" },
                    description: "Use bar repeat.",
                    getDefaultValue: () => true);
            
            var subs = new Option<bool>(
                    aliases: new string[] { "--submissing", "-s" },
                    description: "Substitute missing chords.",
                    getDefaultValue: () => false);

            var rootCommand = new RootCommand
            {
                inFile,
                outFile,
                startBar,
                barBreak,
                repeat,
                subs
            };

            rootCommand.Description = "Generates output for creating chord sheets at https://www.chordsheet.com/";

            rootCommand.SetHandler(
                async (string arg1, string arg2, int arg3, int arg4, bool arg5, bool arg6) => { 
                    await Process(arg1,arg2,arg3, arg4,arg5,arg6); 
                },
                inFile, outFile, startBar,barBreak,repeat,subs);

            return await rootCommand.InvokeAsync(args);
        }

        /// <summary>
        /// Process song master file into Chrord Sheet file
        /// </summary>
        /// <param name="inFile">Input file name</param>
        /// <param name="outFile">Outut file name</param>
        /// <param name="startBar">Bar to start processing</param>
        /// <param name="barBreak">Number of bars per line default = 4</param>
        /// <param name="repeat">Use repeating sections true or false</param>
        /// <returns></returns>
        static async Task Process(string inFile, string outFile, int startBar, int barBreak, bool repeat, bool subs)
        {
            SongData data = new();
            List<Marker> SectionTimings = new();
            List<Marker> SubSectionTimings = new();
            List<Marker> Chords = new();
            List<Marker> TimeSigs = new();
            string CurrentElement = string.Empty;

            if (!File.Exists(inFile))
            {
                Console.WriteLine($"{inFile ?? "File"} not found");
                return;
            }

            XmlReaderSettings settings = new()
            {
                Async = true
            };

            double dValue;
            int iValue;

            try
            {
                using FileStream stream = File.Open(inFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                using XmlReader reader = XmlReader.Create(stream, settings);

                while (await reader.ReadAsync())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "SongData":
                                    data.Uuid = reader.GetAttribute("uuid") ?? string.Empty;
                                    data.AudioFileName = reader.GetAttribute("audioFilename") ?? string.Empty;
                                    data.SongName = reader.GetAttribute("songName") ?? string.Empty;
                                    data.Version = reader.GetAttribute("version") ?? string.Empty;
                                    data.SongFormatVersion = reader.GetAttribute("songFormatVersion") ?? string.Empty;
                                    Console.WriteLine(data.SongName);
                                    break;
                                case "Sections":
                                    break;
                                case "SectionTimings":
                                    CurrentElement = "SectionTimings";
                                    break;
                                case "SubSectionTimings":
                                    CurrentElement = "SubSectionTimings";
                                    break;
                                case "Chords":
                                    
                                    CurrentElement = "Chords";
                                    break;
                                case "TimeSigs":
                                    CurrentElement = "TimeSigs";
                                    break;
                                case "Marker":
                                    Marker marker = new()
                                    {
                                        MarkerText = reader.GetAttribute("markerText") ?? string.Empty
                                    };
                                    _ = double.TryParse(reader.GetAttribute("startTime"), out dValue);
                                    marker.StartTime = dValue;
                                    _ = double.TryParse(reader.GetAttribute("endTime"), out dValue);
                                    marker.EndTime = dValue;
                                    _ = int.TryParse(reader.GetAttribute("numBars"), out iValue);
                                    marker.NumBars = iValue;

                                    switch (CurrentElement)
                                    {
                                        case "SectionTimings":
                                            if (marker.MarkerText != "Click")
                                                SectionTimings.Add(marker);
                                            break;
                                        case "SubSectionTimings":
                                            SubSectionTimings.Add(marker);
                                            break;
                                        case "Chords":
                                            if (marker.MarkerText == "N") marker.MarkerText = "NC";
                                            marker.MarkerText = marker.MarkerText.Replace(":", "");
                                            // Extract root note and note number
                                            string[] inputParts = marker.MarkerText.Split('/');
                                            if(inputParts.Length > 1)
                                            {
                                                string rootNote = inputParts[0];
                                                int noteNumber = int.Parse(inputParts[1]);
                                                string resultNote = GetMajorScaleNote(rootNote, noteNumber);
                                                marker.MarkerText = inputParts[0] + "/" + resultNote;
                                            }

                                            Chords.Add(marker);
                                            break;
                                        case "TimeSigs":
                                            marker.MarkerText = marker.MarkerText.Replace("/", ":");
                                            TimeSigs.Add(marker);
                                            break;
                                    }

                                    break;

                                case "BarBeat":
                                    BarBeat bar = new();
                                    _ = double.TryParse(reader.GetAttribute("time"), out dValue);
                                    bar.Time = dValue;
                                    _ = int.TryParse(reader.GetAttribute("bar"), out iValue);
                                    bar.Bar = iValue;
                                    _ = int.TryParse(reader.GetAttribute("beat"), out iValue);
                                    bar.Beat = iValue;
                                    if (bar.Bar >= startBar)
                                        BeatTimings.Add(bar);
                                    break;

                            }
                            break;

                        case XmlNodeType.EndElement:
                            CurrentElement = "";
                            break;

                        default:
                            //Console.WriteLine("Other node {0} with value {1}",
                            //                reader.NodeType, reader.Value);
                            break;
                    } // close switch
                } //close while reader

                var timeSigMap = TimeSigs.MapMarkers(BeatTimings);
                var sectionMap = SectionTimings.MapMarkers(BeatTimings);
                var subSectionMap = SubSectionTimings.MapMarkers(BeatTimings);
                var chordMap = Chords.MapMarkers(BeatTimings);

                KeyValuePair<Marker, BarBeat> section;
                KeyValuePair<Marker, BarBeat> currentSection = new();
                int cnt = 0;
                //int bar = 0;

                var missingChords = FindMissingChords(chordMap, startBar);

                foreach (var chord in missingChords)
                {
                    chordMap.Add(chord.Key, chord.Value);
                }

                var barMap = chordMap.GroupBy(b => b.Value).OrderBy(b => b.Key.Bar).ToList();
                List<string> chartList = new();
                StringBuilder temp = new();

                foreach (var bar in barMap)
                {
                    // Glue chords in same bar
                    // ToDo: assign on beat
                    if (bar.Count() > 1)
                    {
                        for (int i = 0; i < bar.Count() - 1; i++)
                        {
                            bar.ElementAt(i).Key.MarkerText += "_";
                        }

                        bar.ElementAt(bar.Count() - 1).Key.MarkerText += " ";
                    }
                    else
                    {
                        bar.ElementAt(0).Key.MarkerText += " ";
                    }

                    foreach (var chord in bar)
                    {
                        if (chord.Value.Bar != 0)
                        {
                            section = sectionMap.Where(s => s.Value == chord.Value).FirstOrDefault();

                            if (section.Key != null)
                            {
                                if (currentSection.Key == null || currentSection.Key != section.Key)
                                {
                                    //Add last group if new section with a partial line
                                    if (temp.Length > 0)
                                    {
                                        chartList.Add(temp.ToString().TrimEnd());
                                        temp.Clear();
                                    }
                                    chartList.Add(":" + section.Key.MarkerText);
                                    currentSection = section;
                                    cnt = 0;
                                }
                            }

                            temp.Append(chord.Key.MarkerText);

                            if (!chord.Key.MarkerText.Contains('_'))
                                cnt++;
                            if (cnt >= barBreak)
                            {
                                chartList.Add(temp.ToString().TrimEnd());
                                cnt = 0;
                                temp.Clear();
                            }
                        }                        
                    }
                }

                //Add last group if we exited above with a partial line
                if(temp.Length > 0)
                {
                    chartList.Add(temp.ToString().TrimEnd());
                }

                if(subs)
                {
                    string lastChord = string.Empty;
                    
                    for(int l=0; l<chartList.Count(); l++)
                    {
                        var chords = chartList[l].Split(' ');

                        for(int c = 0; c< chords.Length; c++)
                        {
                            if (chords[c] != "...")
                                lastChord = chords[c];
                            else
                                chords[c] = lastChord;
                        }
                        chartList[l] = string.Join(" ", chords);
                     
                    }
                }

                var repeats = GetSequentialDuplicateCounts(chartList);
                if (repeat)
                {
                    foreach (var (Element, Count) in repeats)
                    {
                        if (Count > 1)
                        {
                            Console.WriteLine($"({Element}) x{Count}");
                        }
                        else
                        {
                            Console.WriteLine(Element);
                        }

                    }
                }
                else
                {
                    foreach (var line in chartList)
                    {
                        Console.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
            }
        }

        /// <summary>
        /// Find bars with no chords and create a default no chord key value pair
        /// </summary>
        /// <param name="chordMap">The Song MAster xchord map</param>
        /// <param name="minBar"></param>
        /// <returns></returns>
        static IEnumerable<KeyValuePair<Marker, BarBeat>> FindMissingChords(Dictionary<Marker, BarBeat> chordMap, int minBar)
        {
            var maxBar = chordMap.Values.Max(obj => obj.Bar);

            var missingKeys = Enumerable.Range(minBar, maxBar - minBar + 1)
                                         .Except(chordMap.Values.Select(obj => obj.Bar));

            return missingKeys.Select(missingKey =>
            {
                var bar = BeatTimings.Where(b => b.Bar == missingKey).FirstOrDefault();
                var missingBar = new BarBeat { Bar = missingKey, Beat = 1, Time = bar!.Time };
                var missingChord = new Marker() { NumBars = 1, MarkerText = "...", StartTime = bar!.Time, EndTime = bar!.Time };
                return new KeyValuePair<Marker, BarBeat>(missingChord, missingBar);
            });
        }
        /// <summary>
        /// Get repeating sections to use if use repeat is enabled
        /// </summary>
        /// <param name="collection">Collection of bar line to parse</param>
        /// <returns></returns>
        static IEnumerable<(string Element, int Count)> GetSequentialDuplicateCounts(List<string> collection)
        {
            if (collection == null || collection.Count == 0)
            {
                yield break;
            }

            string currentElement = collection[0];
            int currentCount = 1;

            for (int i = 1; i < collection.Count; i++)
            {
                if (collection[i] == currentElement)
                {
                    // Increment the count for sequential duplicates
                    currentCount++;
                }
                else
                {
                    // Return the count for the current sequential duplicates
                    yield return (currentElement, currentCount);

                    // Reset for the next element
                    currentElement = collection[i];
                    currentCount = 1;
                }
            }

            // Return the count for the last sequential duplicates
            yield return (currentElement, currentCount);
        }

        static string GetMajorScaleNote(string rootNote, int noteNumber)
        {
            // Define the major scale pattern
            int[] majorScalePattern = { 0, 2, 4, 5, 7, 9, 11, 12 };

            // Find the index of the root note in the list of all notes
            int rootIndex = Array.IndexOf(NoteNames, rootNote.ToUpper());

            // Calculate the index of the target note in the major scale
            int targetIndex = (rootIndex + majorScalePattern[noteNumber - 1]) % NoteNames.Length;

            // Return the corresponding note
            return NoteNames[targetIndex];
        }

        // List of all note names
        static string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        /* ---------------------------- */
    }
}