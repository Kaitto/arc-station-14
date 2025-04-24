using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Content.Shared.Arcade.SharedCustomGameArcadeComponent;

namespace Content.Server.Arcade.CustomGame
{
    public sealed class CustomGameState
    {
        public string mapName = "default";
        public Dictionary<EntityUid, EntityData> entities = new();
        public CustomGameState() { }
    }
}
