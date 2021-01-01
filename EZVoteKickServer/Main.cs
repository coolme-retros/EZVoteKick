using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;
using System.Configuration;
using MySqlConnector;
using System.Data;
using Config.Reader;
using System.IO;
using System.Reflection;

namespace EZVoteKickServer
{
    class Main : BaseScript
    {
        public iniconfig iniconfig { get; set; } = new iniconfig("ezvotekick", "config.ini");

        private DateTime _lastAdvTime;
        private DateTime _nextAdvTime;
        private DateTime _adshowStart;
        private DateTime _adshowEnd;
        private int sec;
        private bool notified;
        private bool adshowing;


        private string adv = "Thanks for using EZVoteKick created by CoolMe Retros. This is a free version of the software. By allowing this to run you have agreed to allow ads to be displayed.";
        public Main()
        {

            EventHandlers["onResourceStart"] += new Action<string>(CheckHtml);
            InitilizeAdministrators();
            RegisterCommands();
            VoteKickHandler.VoteKickTimer();
            IniStQL();
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnPlayerConnecting);
            Debug.WriteLine(adv);
            _lastAdvTime = DateTime.Now;
            _nextAdvTime = _lastAdvTime.AddSeconds(60);

            Tick += new Func<Task>(OnTick);


        }
        private async Task OnTick()
        {



        }
        void CheckHtml(string resourceName)
        {


        }
        private static void IniStQL()
        {
            //Soon to be removed
            if (!VoteKickHandler.EZVoteKickEnabled)
                return;
            string connectionString = @"server=localhost;userid=root;password=;database=fivem";
            MySqlConnection mySqlConnection = new MySqlConnection(connectionString);
            mySqlConnection.Open();
            MySqlCommand mySqlCommand = new MySqlCommand("SELECT identifier FROM admins", mySqlConnection);
            MySqlDataAdapter da = new MySqlDataAdapter(mySqlCommand);
            DataTable dataTable = new DataTable();
            da.Fill(dataTable);
            mySqlConnection.Close();
            mySqlConnection.Dispose();
            foreach (DataRow row in dataTable.Rows)
            {
                var identifier = Convert.ToString(row["identifier"]);
                VoteKickHandler.AdminIdentifiers.Add(identifier);
            }

        }
        public static void InitilizeAdministrators()
        {
            if (!VoteKickHandler.EZVoteKickEnabled)
                return;
            /*string connectionString = @"server=localhost;userid=root;password=;database=fivem";
            MySqlConnection connection = null;
            connection = new MySqlConnection(connectionString);
            connection.Open();
            MySqlCommand mySqlCommand = new MySqlCommand("SELECT identifier FROM admins;", connection);
            using (MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader())
            {
                if (mySqlDataReader.Read())
                {
                    foreach(var id in mySqlDataReader)
                    {
                        VoteKickHandler.AdminIdentifiers.Add(id.ToString());
                        Debug.WriteLine(id.ToString());
                        Debug.WriteLine("Done");
                    }
                }
            }*/
        }
        private async void OnPlayerConnecting([FromSource] Player player, string playerName, dynamic setKickReason, dynamic deferrals)
        {
            if (!VoteKickHandler.EZVoteKickEnabled)
                return;

            deferrals.defer();

            // mandatory wait!
            await Delay(0);

            var licenseIdentifier = player.Identifiers["license"];
            foreach (var id in player.Identifiers)
            {
                Debug.WriteLine(id);
            }

            Debug.WriteLine($"A player with the name {playerName} (Identifier: [{licenseIdentifier}]) is connecting to the server.");

            //deferrals.update($"Hello {playerName}, your license [{licenseIdentifier}] is being checked");

            // Checking ban list
            // - assuming you have a function called IsBanned of type Task<bool>
            // - normally you'd do a database query here, which might take some time
            List<string> ids = player.Identifiers.ToList();
            foreach (var id in ids)
            {
                if (VoteKickHandler.KickedIds.Contains(id))
                {
                    deferrals.done($"You were recently vote kicked from this server. It will expire in 3 minutes or less.");
                }
            }


            deferrals.done();
        }
        public void RegisterCommands()
        {
            if (!VoteKickHandler.EZVoteKickEnabled)
                return;
            RegisterCommand("vkick", new Action<int, List<object>, string>((source, args, raw) =>
            {
                Player target =
                Players[args[0].ToString()];
                Player initer = Players[source];
                if (DateTime.Now < VoteKickHandler.NextKickTime)
                {
                    SendChatMessage("[VOTEKICK]", "A recent vote kick has passed. Please wait before another one can be initiated", 149, 50, 168, initer);
                    return;
                }
                //bool voteKickLimit = Convert.ToBoolean(ConfigurationManager.AppSettings["VoteKickLimitEnabled"]);
                //int voteKickCount = int.Parse(ConfigurationManager.AppSettings["VoteKickLimitCount"]);
                // string data = Function.Call<string>(Hash.LOAD_RESOURCE_FILE, "".Length, "config.ini");
                //To prevent the user seeing debugging errors. We will set a condition if the player does not exist
                if (target == null)
                {
                    SendChatMessage("[VOTEKICK]", "Player not found. Please re-check and try again.", 149, 50, 168, initer);

                    return;
                }
                //var adminids = ConfigurationManager.AppSettings["AdministratorsIdentifiers"];
                var adminid = "fivem:" + target.Identifiers["fivem"];
                if (VoteKickHandler.AdminIdentifiers.Contains(adminid))
                {
                    SendChatMessage("[VOTEKICK]", "You cannot vote kick that user as they are an administrator!", 149, 50, 168, initer);
                    return;
                }
                //Lets check for online players. If its under four players, we will prevent votekick to prevent abuse
                var kickLimitEnabled = Convert.ToBoolean(iniconfig.GetStringValue("MISC".ToLower(), "VoteKickLimit".ToLower(), "false"));
                var kickLimitCount = iniconfig.GetIntValue("MISC".ToLower(), "VoteKickLimitCount".ToLower(), 4);
                var playersToKickPercent = iniconfig.GetStringValue("MAIN".ToLower(), "VoteKickPlayersPercent".ToLower(), "50");
                var withDecimal = Convert.ToDouble("." + playersToKickPercent);
                if (kickLimitEnabled)
                {
                    if (Players.Count() < kickLimitCount)
                    {
                        SendChatMessage("[VOTEKICK]", $"To prevent abuse, a Vote Kick cannot be initiated with less than {kickLimitCount} online players.", 149, 50, 168, initer);
                        return;
                    }
                }
                //We will also do a db check to prevent vote kicks on administrators
                var msg = new Dictionary<string, object>
                {
                    ["color"] = new[] { 149, 50, 168 },
                    ["args"] = new[] { "[VOTEKICK]", $"{initer.Name} has initiated a Vote Kick on {target.Name}.  Type /vkyes to vote yes or /vkno to vote no. VoteKick will end in in 1 minute." }
                };
                TriggerClientEvent("chat:addMessage", msg);
                VoteKickHandler.TargetPlayer = target;
                VoteKickHandler.InitiatedPlayer = initer;
                VoteKickHandler.VoteKickTime = 60;
                VoteKickHandler.VotesToKick = Players.Count() * Convert.ToInt32(Math.Round(withDecimal));
                VoteKickHandler.VoteKickActive = true;
                VoteKickHandler.StartTimer();
            }), false);
            RegisterCommand("vkyes", new Action<int, List<object>, string>((source, args, raw) =>
            {
                Player client = Players[source];
                if (VoteKickHandler.VoteKickActive)
                {
                    VoteKickHandler.YesVotes++;
                    VoteKickHandler.VotesToKick--;

                    var msg = new Dictionary<string, object>
                    {
                        ["color"] = new[] { 149, 50, 168 },
                        ["args"] = new[] { "[VOTEKICK]", "Your vote has been casted!" }
                    };
                    //TriggerEvent("chat:addMessage", msg);
                    var msg1 = new Dictionary<string, object>
                    {
                        ["color"] = new[] { 149, 50, 168 },
                        ["args"] = new[] { "[VOTEKICK]", $"{VoteKickHandler.VotesToKick} votes are needed to kick {VoteKickHandler.TargetPlayer.Name} \n {VoteKickHandler.YesVotes}/{VoteKickHandler.VotesToKick}" }
                    };
                    TriggerClientEvent(client, "chat:addMessage", msg);
                    TriggerClientEvent("chat:addMessage", msg1);
                }
                else
                {
                    SendChatMessage("[VOTEKICK]", "There is no current Vote Kick in progress", 149, 50, 168, client);
                }
                if (VoteKickHandler.VoteKickActive)
                {
                    if (VoteKickHandler.YesVotes > VoteKickHandler.VotesToKick)
                    {
                        //We call datetime here to prevents an uncommon error
                        DateTime currentTime = DateTime.Now;
                        VoteKickHandler.KickedIds.AddRange(VoteKickHandler.TargetPlayer.Identifiers);
                        foreach (var pids in VoteKickHandler.TargetPlayer.Identifiers)
                        {
                            VoteKickHandler.KickedTime.TryAdd(pids, currentTime);
                        }
                        VoteKickHandler.TargetPlayer.Drop("You have been voted to be kicked from the server. You will not be able to return for 3 minutes.");
                        VoteKickHandler.VoteKickActive = false;
                        VoteKickHandler.YesVotes = 0;
                        VoteKickHandler.NoVotes = 0;
                        VoteKickHandler.InitiatedPlayer = null;
                        VoteKickHandler.TargetPlayer = null;
                        VoteKickHandler.VoteKickTime = 0;

                    }
                }
            }), false);
            RegisterCommand("vkno", new Action<int, List<object>, string>((source, args, raw) =>
            {
                Player client = Players[source];
                if (VoteKickHandler.VoteKickActive)
                {
                    VoteKickHandler.NoVotes++;

                    var msg = new Dictionary<string, object>
                    {
                        ["color"] = new[] { 149, 50, 168 },
                        ["args"] = new[] { "[VOTEKICK]", "Your vote has been casted!" }
                    };
                    var msg1 = new Dictionary<string, object>
                    {
                        ["color"] = new[] { 149, 50, 168 },
                        ["args"] = new[] { "[VOTEKICK]", $"{VoteKickHandler.VotesToKick} votes are needed to kick {VoteKickHandler.TargetPlayer.Name} \n {VoteKickHandler.YesVotes}/{VoteKickHandler.VotesToKick}" }
                    };
                    TriggerClientEvent(client, "chat:addMessage", msg);
                    TriggerClientEvent("chat:addMessage", msg1);
                }
                if (VoteKickHandler.VoteKickActive)
                {
                    if (VoteKickHandler.YesVotes > VoteKickHandler.VotesToKick)
                    {
                        DateTime currentTime = DateTime.Now;
                        VoteKickHandler.KickedIds.AddRange(VoteKickHandler.TargetPlayer.Identifiers);
                        foreach (var pids in VoteKickHandler.TargetPlayer.Identifiers)
                        {
                            VoteKickHandler.KickedTime.TryAdd(pids, currentTime);
                        }
                        VoteKickHandler.TargetPlayer.Drop("You have been voted to be kicked from the server. You will not be able to return for 3 minutes.");
                        VoteKickHandler.VoteKickActive = false;
                        VoteKickHandler.YesVotes = 0;
                        VoteKickHandler.NoVotes = 0;
                        VoteKickHandler.InitiatedPlayer = null;
                        VoteKickHandler.TargetPlayer = null;
                        VoteKickHandler.VoteKickTime = 0;
                    }
                }
                else
                {
                    SendChatMessage("[VOTEKICK]", "There is no current Vote Kick in progress", 149, 50, 168, client);
                }
            }), false);
            RegisterCommand("vkend", new Action<int, List<object>, string>((source, args, raw) =>
            {
                /*Player client = Players[source];
                var adminid = "fivem:" + client.Identifiers["fivem"];
                if (!VoteKickHandler.AdminIdentifiers.Contains(adminid))
                    return;*/


            }), false);


        }
        public static void SendChatMessage(string title, string message, int r, int g, int b, Player player)
        {
            if (!VoteKickHandler.EZVoteKickEnabled)
                return;
            var msg = new Dictionary<string, object>
            {
                ["color"] = new[] { r, g, b },
                ["args"] = new[] { title, message }
            };
            TriggerClientEvent(player, "chat:addMessage", msg);
        }

    }
}
