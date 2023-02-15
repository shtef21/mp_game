using System.Net.WebSockets;
using tps_game.Code.Games;

namespace tps_game.Code
{
    // A class used for various tasks
    public class Static
    {
        public static Random Random = new Random();


#if DEBUG
        public static bool dbDeleteSetting = true;
#else
        public static bool deletePreviousDb = false;
#endif
    }
}
