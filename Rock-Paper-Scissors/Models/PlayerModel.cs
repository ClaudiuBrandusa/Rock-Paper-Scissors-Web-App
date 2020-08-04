using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rock_Paper_Scissors.Models
{
    public class PlayerModel
    {
        public ConnectionData Connection { get; set; }
        public bool IsWaiting { get; set; } // true after submits response and false after gets the result
        public int Choice { get; set; }
        public bool Chose { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as PlayerModel;
            return Connection.Equals(other.Connection);
        }

        public override int GetHashCode()
        {
            return Connection.GetHashCode();
        }
    }
}
