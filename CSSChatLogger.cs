using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using static CounterStrikeSharp.API.Core.Listeners;
using Microsoft.Extensions.Logging;
using CSSChatLogger.Config;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;

namespace CSSChatLogger
{
    public class CSSChatLogger : BasePlugin
    {
        public override string ModuleName => "CSS Chat Logger (Log Chat to UDP server)";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "Reo";
        public override string ModuleDescription => "Logs messages to UDP server";

        private readonly ILogger<CSSChatLogger> _logger;

        public CSSChatLogger(ILogger<CSSChatLogger> logger)
        {
            _logger = logger;
        }

        public override void Load(bool hotReload)
        {
            Configs.Load(ModuleDirectory);
            Configs.Shared.CookiesModule = ModuleDirectory;
            AddCommandListener("say", OnEventPlayerChat);
            AddCommandListener("say_team", OnEventPlayerChat);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            _logger.LogInformation("CSS Chat Logger loaded");
        }

        public void OnMapStart(string Map)
        {
        }

        public HookResult OnEventPlayerChat(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.IsBot) return HookResult.Handled;
            var eventmessage = info.GetArg(1);

            if (player == null || !player.IsValid) return HookResult.Continue;
            var vplayername = player.PlayerName;
            var steamId3 = player.AuthorizedSteamID?.SteamId3 ?? "InvalidSteamID";
            var playerid = player.SteamID;
            if (string.IsNullOrWhiteSpace(eventmessage)) return HookResult.Continue;
            string trimmedMessageStart = eventmessage.TrimStart();
            string message = trimmedMessageStart.TrimEnd();

            if (!Globals.Client_Text1.ContainsKey(playerid))
            {
                Globals.Client_Text1.Add(playerid, message);
            }
            if (!Globals.Client_Text2.ContainsKey(playerid))
            {
                Globals.Client_Text2.Add(playerid, string.Empty);
            }

            if (Globals.Client_Text1.ContainsKey(playerid))
            {
                Globals.Client_Text1[playerid] = Globals.Client_Text2[playerid];
            }
            if (Globals.Client_Text2.ContainsKey(playerid))
            {
                Globals.Client_Text2[playerid] = message;
            }
            string formattedMessage = $"RL {DateTime.Now:MM/dd/yyyy - HH:mm:ss.fff} - \"{vplayername}<{player.UserId}><{steamId3}><{player.Team}>\" say \"{message}\"";
            _logger.LogInformation($"Formatted message: {formattedMessage}");

            if (Configs.GetConfigData().UDPServerIP != null)
            {
                _logger.LogInformation("Sending message to UDP server.");
                try
                {
                    Task.Run(async () =>
                    {
                        await Helper.SendToUDPServer(Configs.GetConfigData().UDPServerIP, Configs.GetConfigData().UDPServerPort, formattedMessage, _logger);
                    }).Wait();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("UDPServerIP is not configured.");
            }

            return HookResult.Continue;
        }

        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (@event == null) return HookResult.Continue;
            var player = @event.Userid;

            if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return HookResult.Continue;
            var playerid = player.SteamID;

            Globals.Client_Text1.Remove(playerid);
            Globals.Client_Text2.Remove(playerid);

            return HookResult.Continue;
        }

        public void OnMapEnd()
        {
            Helper.ClearVariables();
        }

        public override void Unload(bool hotReload)
        {
            Helper.ClearVariables();
        }
    }
}
