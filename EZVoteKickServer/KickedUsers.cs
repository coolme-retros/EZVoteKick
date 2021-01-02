using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZVoteKickServer
{
    public class KickedUsers
    {
        public int KickedId { get; set; }
        public bool Kicked { get; set; }
        public string FiveMId { get; set; }
        public string SteamId { get; set; }
        public string xbl { get; set; }
        public string LiveId { get; set; }
        public string Discord { get; set; }
        public string Ip { get; set; }
        public string License { get; set; }
        public string TimeKicked { get; set; }
    }
}
