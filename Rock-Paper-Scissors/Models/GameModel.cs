using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rock_Paper_Scissors.Models
{
    public class GameModel
    {
        public PlayerModel Player1 { get; set; }

        public PlayerModel Player2 { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as GameModel;
            return Player1.Equals(other.Player1) && Player2.Equals(other.Player2);
        }

        public override int GetHashCode()
        {
            return Player1.GetHashCode() ^ Player2.GetHashCode();
        }
    }
}
