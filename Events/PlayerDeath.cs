using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Eventing;
using OpenMod.API.Users;
using OpenMod.Core.Users;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using UnityEngine;

namespace DeathMessages.Events;

public class PlayerDeath : IEventListener<UnturnedPlayerDeathEvent>
{
    private readonly IStringLocalizer _StringLocalizer;
    private readonly IUserManager _UserManager;
    private readonly IConfiguration _Configuration;

    public PlayerDeath(
            IStringLocalizer stringLocalizer, 
            IConfiguration configuration,
            IUserManager userManager)
    {
        _StringLocalizer = stringLocalizer;
        _UserManager = userManager;
        _Configuration = configuration;
    }

    public Task HandleEventAsync(object? sender, UnturnedPlayerDeathEvent @event)
    {
        switch (@event.DeathCause)
        {
            case EDeathCause.PUNCH:
                _ = HandlePunch(@event);
                break;
            case EDeathCause.GUN:
            case EDeathCause.MELEE:
                _ = HandleItem(@event);
                break;
            default:
                HandleDefault(@event);
                break;
        }

        return Task.CompletedTask;
    }

    private void HandleDefault(UnturnedPlayerDeathEvent @event)
    {
        object format = new
        {
            Victim = @event.Player.SteamPlayer.playerID.characterName
        };

        string? icon = _Configuration.GetValue<string>("IconUrl") ?? null;
        ChatManager.serverSendMessage(
                _StringLocalizer[$"Deaths:{@event.DeathCause}", format], 
                Color.white, 
                null, 
                null, 
                EChatMode.GLOBAL, 
                icon, 
                true
            );
    }

    private async Task HandlePunch(UnturnedPlayerDeathEvent @event)
    {
        UnturnedUser? user = 
            (UnturnedUser?)await _UserManager.FindUserAsync(KnownActorTypes.Player, @event.Instigator.ToString(), UserSearchMode.FindById);

        object format = new
        {
            Victim = @event.Player.SteamPlayer.playerID.characterName,
            Killer = user?.Player.SteamPlayer.playerID.characterName ?? "Player",
        };

        string? icon = _Configuration.GetValue<string>("IconUrl") ?? null;
        ChatManager.serverSendMessage(
                _StringLocalizer[$"Deaths:{@event.DeathCause}", format], 
                Color.white, 
                null, 
                null, 
                EChatMode.GLOBAL, 
                icon, 
                true
            );
    }

    private async Task HandleItem(UnturnedPlayerDeathEvent @event)
    {
        UnturnedUser? user = 
            (UnturnedUser?)await _UserManager.FindUserAsync(KnownActorTypes.Player, @event.Instigator.ToString(), UserSearchMode.FindById);

        if (user == null)
        {
            return;
        }

        Useable useable = user.Player.Player.equipment.useable;

        string item;
        if (useable is UseableGun gun)
        {
            item = gun.equippedGunAsset.name;
        }
        else if (useable is UseableMelee melee)
        {
            item = melee.equippedMeleeAsset.name;
        }
        else
        {
            return;
        }

        object format = new
        {
            Victim = @event.Player.SteamPlayer.playerID.characterName,
            Killer = user?.Player.SteamPlayer.playerID.characterName ?? "Player",
            Gun = item
        };

        string? icon = _Configuration.GetValue<string>("IconUrl") ?? null;
        ChatManager.serverSendMessage(
                _StringLocalizer[$"Deaths:{@event.DeathCause}", format], 
                Color.white, 
                null, 
                null, 
                EChatMode.GLOBAL, 
                icon, 
                true
            );
    }
}
