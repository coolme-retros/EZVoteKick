using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core.UI;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Dynamic;

namespace EZVoteKickClient
{
    class Main : BaseScript
    {
        public Main()
        {

            EventHandlers["onClientResourceStart"] += new Action<string>(AddSugestions);
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(CheckVoteKick);
            EventHandlers["nui:off"] += new Action(offnui);
            EventHandlers["nui:on"] += new Action(onnui);
            EventHandlers["playerConnecting"] += new Action(offnui);
            EventHandlers["votekick:notkicked"] += new Action<Player>(AlertNotKicked);
            RegisterClientCommands();
        }
       
        void AlertNotKicked(Player player)
        {
            Screen.ShowNotification($"Vote time has expired. Either not enough players said yes, or not enough players decided to vote. {player.Name} has not been kicked.");
        }
        public async void CheckVoteKick([FromSource] Player player, string playerName, dynamic setKickReason, dynamic deferrals)
        {

        }
        void AddSugestions(string resourceName)
        {

            TriggerEvent("chat:addSuggestion", "/vkick", "Initiates a Vote Kick on the target player. Note the system will not allow a votekick with less than 4 players.", new[]
            {
                new {name = "Username", help = "The person you want to try and vote kick"}
            });
            TriggerEvent("chat:addSuggestion", "/vkyes", "Votes yes to an ongoing vote kick.");
            TriggerEvent("chat:addSuggestion", "/vkno", "Votes no to an ongoing vote kick.");
        }
        void offnui()
        {
            SendNuiMessage("{ \"type\": \"ui\", \"display\": false }");
        }
        void onnui()
        {
            SendNuiMessage("{ \"type\": \"ui\", \"display\": true }");
        }
        void RegisterClientCommands()
        {
            RegisterCommand("on", new Action<int, List<object>, string>((source, args, raw) =>
            {
                TriggerEvent("nui:on");


            }), false);
            RegisterCommand("off", new Action<int, List<object>, string>((source, args, raw) =>
            {
                TriggerEvent("nui:off");

            }), false);
        }
        public void RegisterEventHandler(string name, Delegate action)
        {
            try
            {
                EventHandlerDictionary eventHandlers = EventHandlers;
                eventHandlers[name] += action;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public static void SendChatMessage(string title, string message, int r, int g, int b)
        {
            var msg = new Dictionary<string, object>
            {
                ["color"] = new[] { r, g, b },
                ["args"] = new[] { title, message }
            };
            TriggerEvent("chat:addMessage", msg);
        }
        private void RegisterNUICallback(string msg, Func<IDictionary<string, object>, CallbackDelegate, CallbackDelegate> callback)
        {
            API.RegisterNuiCallbackType(msg);

            EventHandlers[$"__cfx_nui:{msg}"] += new Action<ExpandoObject, CallbackDelegate>((body, resultCallback) =>
            {
                CallbackDelegate err = callback.Invoke(body, resultCallback);

                //if (!string.IsNullOrWhiteSpace(err)) TriggerServerEvent("_chat:messageEntered", Game.Player.Name, new byte[] { 0, 0x99, 255 }, "null");
                //Debug.WriteLine("error during NUI callback " + msg + ": " + err);
            });
        }

        private void SendNUIMessage(string message)
        {
            API.SendNuiMessage(message);
        }

    }



}


