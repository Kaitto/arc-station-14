using Content.Server.Arcade.CustomGame;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Examine;
using Content.Shared.Tools.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using System;
using System.Linq;
using static Content.Shared.Arcade.SharedCustomGameArcadeComponent;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
public sealed class CustomGameArcadeSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    public CustomGameState globalGame = new();
    bool init = false;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CustomGameArcadeComponent, AfterActivatableUIOpenEvent>(OnAfterUIOpen);
        SubscribeLocalEvent<CustomGameArcadeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CustomGameArcadeComponent, ExaminedEvent>(OnExamine);

        Subs.BuiEvents<CustomGameArcadeComponent>(CustomGameUiKey.Key, subs =>
        {
            //subs.Event<BoundUIClosedEvent>(OnAfterUiClose);
            subs.Event<CustomGameUpdateMessage>(OnPlayerAction);
            subs.Event<CustomGamePlayerActionMessage>(OnPlayerAction);
        });

    }
    private void OnPlayerAction(EntityUid uid, CustomGameArcadeComponent component, CustomGamePlayerActionMessage msg)
    {
        if (!CustomGameUiKey.Key.Equals(msg.UiKey)) return; // not sure why this would happen
        //if (msg.Actor != component.Player) return;
        if (component.Player == null) return;
        EntityUid actor = component.Player.Value;
        switch (msg.PlayerAction)
        {
            case PlayerAction.RequestData:
                SendEntities(uid, actor);
                break;
            default:
                break;
        }
    }
    private void OnPlayerAction(EntityUid uid, CustomGameArcadeComponent component, CustomGameUpdateMessage msg)
    {
        if (!CustomGameUiKey.Key.Equals(msg.UiKey)) return; // not sure why this would happen
        //if (msg.Actor != component.Player) return;
        if (component.Player == null) return;
        EntityUid actor = component.Player.Value;
        foreach (var e in msg.EntityData)
        {
            //update this machine character
            
            if (GetEntity(e.Id).Equals(uid))
            {
                //add the character if it doesn't exists
                EnsureMachineData(uid);
                //just replace it for now. no checks
                globalGame.entities[uid] = e;
            }
        }
        //should update all machines, not just this one
        var query = EntityQueryEnumerator<CustomGameArcadeComponent>();
        while (query.MoveNext(out var quid, out var cmp))
        {
            //only update other machines
            if (!uid.Equals(quid)) SendEntities(quid);
        }
    }
    private void EnsureMachineData(EntityUid uid)
    {
        if (!globalGame.entities.ContainsKey(uid))
        {
            var chr = new EntityData();
            var id = uid; //use the arcade as key
            chr.Id = GetNetEntity(uid);
            globalGame.entities[uid] = chr;
        }
    }
    private void OnAfterUIOpen(EntityUid uid, CustomGameArcadeComponent component, AfterActivatableUIOpenEvent args)
    {
        if (component.Player == null)
        {
            component.Player = args.Actor;
        } else
        {
            component.Spectators.Add(args.Actor);
        }
        EnsureMachineData(uid);
    }
    private void OnExamine(EntityUid uid, CustomGameArcadeComponent component, ExaminedEvent args)
    {
        Log.Info("customromgame being examined");
        _chat.TrySendInGameICMessage(uid, "Bonk!", InGameICChatType.Speak, hideChat: true);
    }
    private void OnComponentInit(EntityUid uid, CustomGameArcadeComponent component, ComponentInit args)
    {
        Log.Info("component is being init");
        if (!init)
        {
            //no dummy, use other machines to spawn characters
            ////dummy entity for testing
            //var dummy = new EntityData();
            //dummy.position = new Vector3(0, 0, 0);
            ////As far i know, this wont get replicated at all, and i just need a unique key for now.
            //var id = Spawn(dummy.prototype);
            //dummy.Id = GetNetEntity(id);
            //globalGame.entities[id] = dummy;
            //Log.Info("Create dummy entity for customromgame");
            init = true;
        }
        //component.Game = new(uid);
    }
    private void SendEntities(EntityUid uid, EntityUid actor)
    {
        Log.Info("sending entity list for customromgame");
        _uiSystem.ServerSendUiMessage(uid, CustomGameUiKey.Key, new CustomGameUpdateMessage(globalGame.entities.Values.ToList()), actor);
    }
    private void SendEntities(EntityUid uid)
    {
        Log.Info("sending entity list for customromgame");
        _uiSystem.ServerSendUiMessage(uid, CustomGameUiKey.Key, new CustomGameUpdateMessage(globalGame.entities.Values.ToList()));
    }
}
