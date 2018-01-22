using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using System.IO;
using System.Collections;

namespace MsBuild.ProtocolBuffers
{
    internal class Directories : IEnumerable<KeyValuePair<DirectoryPair, List<string>>>
    {
        private Dictionary<DirectoryPair, List<string>> Dict { get; } = new Dictionary<DirectoryPair, List<string>>();

        public IEnumerable<string> Files(DirectoryPair directoryPair)
            => Dict.TryGetValue(directoryPair, out List<string> value) ? value : Enumerable.Empty<string>();

        public void Add(DirectoryPair pair, string file)
        {
            if (Dict.TryGetValue(pair, out List<string> value))
                value.Add(file);
            else
                Dict.Add(pair, new List<string>(Enumerable.Repeat(file, 1)));
        }

        public void Add(ITaskItem item)
        {
            var itemSpec = item.ItemSpec;
            var input = item.GetMetadata("RelativeDir") ?? Path.GetDirectoryName(itemSpec);
            var output = Path.GetDirectoryName(item.GetMetadata("OutputSpec"));
            var directoryPair = new DirectoryPair(input, output);
            var file = Path.GetFileName(itemSpec);
            Add(directoryPair, file);
        }

        public IEnumerator<KeyValuePair<DirectoryPair, List<string>>> GetEnumerator() 
            => ((IEnumerable<KeyValuePair<DirectoryPair, List<string>>>)Dict).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<KeyValuePair<DirectoryPair, List<string>>>)Dict).GetEnumerator();
    }

    internal struct DirectoryPair : IEquatable<DirectoryPair>
    {
        public string InputDir { get; }
        public string OutputDir { get; }

        public DirectoryPair(string inputDir, string outputDir)
        {
            InputDir = inputDir ?? throw new ArgumentNullException(nameof(inputDir));
            OutputDir = outputDir ?? throw new ArgumentNullException(nameof(outputDir));
        }

        public override string ToString()
        {
            return $"{InputDir} -> {OutputDir}";
        }

        public override bool Equals(object obj)
        {
            return Equals((DirectoryPair)obj);
        }

        public bool Equals(DirectoryPair other)
        {
            return other.InputDir == InputDir && other.OutputDir == OutputDir;
        }

        public override int GetHashCode()
        {
            return InputDir.GetHashCode() ^ OutputDir.GetHashCode();
        }
    }
}
