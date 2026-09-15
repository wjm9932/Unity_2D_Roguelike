using UnityEngine;

namespace Game.Runtime
{
    public class Game
    {
        public static Game Instance { get; private set; }

        public static void Initialize()
        {
            Physics2D.simulationMode = SimulationMode2D.Update;
        }
    }
}
