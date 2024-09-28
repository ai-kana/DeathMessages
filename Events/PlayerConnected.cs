using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Eventing;
using OpenMod.API.Users;
using OpenMod.Unturned.Players.Connections.Events;
using SDG.Unturned;
using UnityEngine;

namespace DeathMessages.Events;

public class PlayerConnected : IEventListener<UnturnedPlayerConnectedEvent>
{
    private readonly IStringLocalizer _StringLocalizer;
    private readonly IUserManager _UserManager;
    private readonly IConfiguration _Configuration;

    public PlayerConnected(
            IStringLocalizer stringLocalizer, 
            IConfiguration configuration,
            IUserManager userManager)
    {
        _StringLocalizer = stringLocalizer;
        _UserManager = userManager;
        _Configuration = configuration;
    }

    public Task HandleEventAsync(object? sender, UnturnedPlayerConnectedEvent @event)
    {
        object format = new
        {
            Player = @event.Player.SteamPlayer.playerID.characterName
        };

        string? icon = _Configuration.GetValue<string>("IconUrl") ?? null;
        ChatManager.serverSendMessage(
                _StringLocalizer[$"Connection:Join", format], 
                Color.white, 
                null, 
                null, 
                EChatMode.GLOBAL, 
                icon, 
                true
            );

        return Task.CompletedTask;
    }
}
