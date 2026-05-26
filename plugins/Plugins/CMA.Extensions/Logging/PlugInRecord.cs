using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMA.Extensions.Logging
{
    internal class PlugInRecord
    {
        public enum LoggingLevel
        {
            Trace = 100000000,
            Information= 100000001,
            Warning= 100000002,
            Error= 100000003
        }

        public string Name { get; set; }
        public LoggingLevel Level { get; set; }
        public string RecordDate { get; set; }
        public string Content { get; set; }

        public override string ToString()
        {
            return $"[{RecordDate}] {Level}: {Name} - {Content}";
        }
    }
}
