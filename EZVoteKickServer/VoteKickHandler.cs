using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using System.Collections.Concurrent;
using Config.Reader;
using CitizenFX.Core.Native;

namespace EZVoteKickServer
{
    public class VoteKickHandler : BaseScript
    {
        public static List<string> KickedIds { get; set; } = new List<string>();
        public static ConcurrentDictionary<string, DateTime> KickedTime { get; set; } = new ConcurrentDictionary<string, DateTime>();
        public static bool VoteKickActive { get; set; }
        public static int VoteKickTime { get; set; }
        public static Player TargetPlayer { get; set; }
        public static Player InitiatedPlayer { get; set; }
        public static int VotesToKick { get; set; }
        public static int YesVotes { get; set; }
        public static int NoVotes { get; set; }
        public static List<string> AdminIdentifiers { get; set; } = new List<string>();
        private static iniconfig iniconfig { get; set; } = new iniconfig("ezvotekick", "config.ini");
        public static DateTime NextKickTime { get; set; }

        public static bool EZVoteKickEnabled { get; set; } = true;

       
        public static async void VoteKickTimer()
        {
        }
        public static async void StartTimer()
        {
            //Timer
            var time1 = iniconfig.GetStringValue("MAIN".ToLower(), "VoteKickTime".ToLower(), "0:1:30");
            var theTImes = time1.Split(':');
            var hour = Convert.ToInt32(theTImes[0]);
            var minutes = Convert.ToInt32(theTImes[1]);
            var seconds = Convert.ToInt32(theTImes[2]);
            DateTime date = DateTime.Now;
            TimeSpan time = new TimeSpan(hour, minutes, seconds);
            DateTime combined = date.Add(time);
            while (VoteKickActive)
            {
                if (DateTime.Now >= combined)
                {
                    var msg = new Dictionary<string, object>
                    {
                        ["color"] = new[] { 255, 0, 0 },
                        ["args"] = new[] { "[VOTEKICK]", $"Vote time has expired. Either not enough players said yes, or not enough players decided to vote. {TargetPlayer.Name} has not been kicked!" }
                    };
                    TriggerClientEvent("chat:addMessage", msg);

                    
                    VoteKickHandler.VoteKickActive = false;
                    VoteKickHandler.YesVotes = 0;
                    VoteKickHandler.NoVotes = 0;
                    VoteKickHandler.InitiatedPlayer = null;
                    VoteKickHandler.TargetPlayer = null;
                    VoteKickHandler.VoteKickTime = 0;
                    var getTimeFailFromIni = iniconfig.GetStringValue("MISC".ToLower(), "VoteKickFailedTime".ToLower(), "0:5:0");
                    var split = getTimeFailFromIni.Split(':');
                    var hour1 = Convert.ToInt32(split[0]);
                    var minute2 = Convert.ToInt32(split[1]);
                    var second2 = Convert.ToInt32(split[2]);
                    var nextKick = DateTime.Now.Add(new TimeSpan(hour1, minute2, second2));
                    NextKickTime = nextKick;
                }
                await Delay(3000);
            }
        }
        public static void AddKickedUser()
        {
            try
            {
                var random = new Random();
                var chars = "0123456789";
                var result = new string(
                    Enumerable.Repeat(chars, 9)
                              .Select(s => s[random.Next(s.Length)])
                              .ToArray());

                var data = Function.Call<string>(Hash.LOAD_RESOURCE_FILE, "ezvotekick", "bans.json");
                
                var kicked = Newtonsoft.Json.JsonConvert.DeserializeObject<List<KickedUsers>>(data);
                var kickedusrs = new KickedUsers()
                {
                    KickedId = Convert.ToInt32(result),
                    Kicked = true,
                    FiveMId = TargetPlayer.Identifiers["fivem"],
                    SteamId = TargetPlayer.Identifiers["steam"],
                    xbl = TargetPlayer.Identifiers["xbl"],
                    LiveId = TargetPlayer.Identifiers["live"],
                    Discord = TargetPlayer.Identifiers["discord"],
                    License = TargetPlayer.Identifiers["license"],
                    Ip = TargetPlayer.Identifiers["ip"]
                };
                Debug.WriteLine(TargetPlayer.Name);
                kicked.Add(kickedusrs);
                var converted = Newtonsoft.Json.JsonConvert.SerializeObject(kicked, Newtonsoft.Json.Formatting.Indented);
                var addKick = Function.Call<bool>(Hash.SAVE_RESOURCE_FILE, "ezvotekick", "bans.json", converted, -1);
                Debug.WriteLine(!addKick ? "Error occured when saving kick." : "");
                Debug.WriteLine(converted);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }
    }
}
