using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace StrafeHUD;

public class Commands
{
    [ConsoleCommand("css_strafestats", "Enable/disable console strafe stats")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void StrafeStatsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!player.IsValid || player.IsBot) return;

        
    }
}