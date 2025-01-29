using CounterStrikeSharp.API.Core;

namespace StrafeHUD;

public class Globals
{
    public static readonly float Frametime = 0.015625f;
    public static Dictionary<int, CCSPlayerController> connectedPlayers = [];
    public static Dictionary<int, PlayerStats> playerStats = [];
}