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
    internal class Directories : IEnumerable<KeyValuePair<string, List<string>>>
    {
        private Dictionary<string, List<string>> Dict { get; } = new Dictionary<string, List<string>>();

        public IEnumerable<string> Files(string directory)
            => Dict.TryGetValue(directory, out List<string> value) ? value : Enumerable.Empty<string>();

        public void Add(string directory, string file)
        {
            if (Dict.TryGetValue(directory, out List<string> value))
                value.Add(file);
            else
                Dict.Add(directory, new List<string>(Enumerable.Repeat(file, 1)));
        }

        public void Add(ITaskItem item)
        {
            var itemSpec = item.ItemSpec;
            var directory = Path.GetDirectoryName(itemSpec);
            var file = Path.GetFileName(itemSpec);
            Add(directory, file);
        }

        public IEnumerator<KeyValuePair<string, List<string>>> GetEnumerator() 
            => ((IEnumerable<KeyValuePair<string, List<string>>>)Dict).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<KeyValuePair<string, List<string>>>)Dict).GetEnumerator();
    }
}
