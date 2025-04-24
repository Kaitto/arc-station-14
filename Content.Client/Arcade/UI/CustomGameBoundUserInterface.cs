using Content.Shared.Arcade;
using Robust.Client.UserInterface;
using System;
using static Content.Shared.Arcade.SharedCustomGameArcadeComponent;

namespace Content.Client.Arcade.UI;

public sealed class CustomGameBoundUserInterface : BoundUserInterface
{
    EntityUid Owner;
    private CustomGameMenu? _menu;
    public CustomGameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        Owner = owner;
    }
    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<CustomGameMenu>();
        _menu.gameScreen.Owner = Owner;
        _menu.gameScreen.dgame.Owner = Owner;
        _menu.OnAction += SendAction;
        _menu.OnUpdate += SendUpdate;

        SendMessage(new CustomGamePlayerActionMessage(PlayerAction.RequestData));
    }
    public void SendAction(PlayerAction action)
    {
        SendMessage(new CustomGamePlayerActionMessage(action));
    }
    public void SendUpdate(EntityData data)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        data.Id = entityManager.GetNetEntity(Owner); // i hope it is the same as the arcade
        SendMessage(new CustomGameUpdateMessage(new List<EntityData>([data])));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        switch (message)
        {
            case CustomGameUpdateMessage msg:
                _menu?.ReceiveMessage(message);
                break;
            default:
                break;
        }
    }
}
