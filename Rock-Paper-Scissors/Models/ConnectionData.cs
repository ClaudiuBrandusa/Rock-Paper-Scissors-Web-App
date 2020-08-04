//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Rock_Paper_Scissors.Models
{
    public class ConnectionData // this model will be used at storing data about the connected users
    {
        [JsonIgnore] // we do not to give the connectionId to the js client in order to avoid fake user impersonation
        public string ConnectionId { get; set; }
        //[JsonProperty("name")]
        [JsonPropertyName("name")]
        public string UserName { get; set; }
        public override bool Equals(object obj)
        {
            var other = obj as ConnectionData;
            return UserName.Equals(other.UserName);
        }

        public override int GetHashCode()
        {
            return UserName.GetHashCode();
        }
    }
}
