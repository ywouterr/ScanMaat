using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanMate.Methods
{
    public class inputQueue
    {
        public Queue<string> _incoming { get; set; }

        public inputQueue()
        {
            _incoming = new Queue<string>();
        }

        public void AddToQ(string fileChange)
        {
            _incoming.Enqueue(fileChange);
        }

        public string Process()
        {
            if (_incoming.Any())
            {
                return _incoming.Dequeue();
            }
            return "Miep";
        }

        public string Inspect()
        {
            if (_incoming.Any())
            {
                return _incoming.Peek();
            }
            return "Moep";
        }
    }
}
