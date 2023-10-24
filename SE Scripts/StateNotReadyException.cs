using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    internal class StateNotReadyException : Exception
    {
        public StateNotReadyException(string message) : base(message)
        {
        }
    }
}
