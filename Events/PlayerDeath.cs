using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Eventing;
using OpenMod.API.Users;
using OpenMod.Core.Users;
using OpenMod.Unturned.Players;
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
                _ = HandleGun(@event);
                break;
            default:
                HandleDefault(@event);
                break;
        }

        return Task.CompletedTask;
    }

    private string FindLocation(UnturnedPlayer player)
    {
        LocationDevkitNodeSystem nodeSystem = LocationDevkitNodeSystem.Get();
        IEnumerable<LocationDevkitNode> nodes = nodeSystem.GetAllNodes();
        
        string name = "";
        float bestDistance = float.PositiveInfinity;
        foreach (LocationDevkitNode node in nodes)
        {
            Vector3 v = node.inspectablePosition - player.Player.transform.position;
            float distance = Vector3.SqrMagnitude(v);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                name = node.locationName;
            }
        }

        return name;
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
            Health = user?.Player.Player.life.health ?? 0,
            Location = FindLocation(@event.Player)
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

    private async Task HandleGun(UnturnedPlayerDeathEvent @event)
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
            Killer = user.Player.SteamPlayer.playerID.characterName,
            Gun = item,
            Health = user.Player.Player.life.health,
            Range = (int)Vector3.Distance(user.Player.Player.transform.position, @event.Player.Player.transform.position),
            Location = FindLocation(@event.Player)
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
