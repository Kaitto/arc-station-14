using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using System.Numerics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.Arcade
{
    public abstract partial class SharedCustomGameArcadeComponent : Component
    {
        [Serializable, NetSerializable]
        public enum CustomGameUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public enum PlayerAction
        {
            NewGame,
            RequestData
        }

        [Serializable, NetSerializable]
        public sealed class CustomGamePlayerActionMessage : BoundUserInterfaceMessage
        {
            public readonly PlayerAction PlayerAction;
            public CustomGamePlayerActionMessage(PlayerAction playerAction)
            {
                PlayerAction = playerAction;
            }
        }

        [Serializable, NetSerializable]
        public sealed class CustomGameUpdateMessage : BoundUserInterfaceMessage
        {
            public readonly List<EntityData> EntityData;
            public CustomGameUpdateMessage(List<EntityData> entityData)
            {
                EntityData = entityData;
            }
        }
        [Serializable, NetSerializable]
        public sealed class EntityData
        {
            public Vector3 position;
            public Vector2 velocity;
            public Angle Angle;
            public NetEntity Id;
            public string prototype = "MobHumanSyndicateAgent";

            public Vector2 inputVelocity;
            public Angle inputRotation;
            public void Tick(float delta)
            {
                var vel = inputVelocity;
                if (inputVelocity.LengthSquared() > 0) vel = vel.Normalized() * 120.0f;

                var rotation = new Quaternion2D(Angle);
                velocity = Vector2.Lerp(velocity, Quaternion2D.RotateVector(rotation, vel), delta * 25.0f);
                rotation = rotation.Set(rotation.Angle + (float) inputRotation * delta);

                Angle = rotation.Angle;
                var dt = velocity * delta;
                position.X += dt.X;
                position.Y += dt.Y;

                velocity *= 0.95f;
            }
        }

    }
}
