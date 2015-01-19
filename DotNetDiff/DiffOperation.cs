using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetDiff
{
    public class DiffOperation
    {
        public DiffOperationType Operation { get; set; }
        public string Path { get; set; }
        public object Value { get; set; }
    }
}
