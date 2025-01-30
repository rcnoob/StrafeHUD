using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using StrafeHUD.Extensions;
using Clientprefs.API;
using CounterStrikeSharp.API.Core.Capabilities;

namespace StrafeHUD;

public class StrafeHUD : BasePlugin
{
    public override string ModuleName => "StrafeHUD";
    public override string ModuleVersion => $"1.0.2";
    public override string ModuleAuthor => "rc https://github.com/rcnoob/";
    public override string ModuleDescription => "A CS2 StrafeHUD plugin";
    
    private readonly PluginCapability<IClientprefsApi> g_PluginCapability = new("Clientprefs");
    private IClientprefsApi ClientprefsApi;
    private int g_iCookieID = -1, g_iCookieID2 = -1;
    
    public required IRunCommand RunCommand;
    private int movementServices;
    private int movementPtr;

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("[StrafeHUD] Loading plugin...");

        // CURRENTLY ONLY LINUX SUPPORT, LEAVING THIS FOR FUTURE MAYBE
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            movementServices = 0;
            movementPtr = 1;
            RunCommand = new RunCommandLinux();
        }
        else
        {
            movementServices = 3;
            movementPtr = 2;
            RunCommand = new RunCommandWindows();
        }

        RunCommand.Hook(OnRunCommand, HookMode.Post);
        
        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            if (@event.Userid!.IsValid)
            {
                var player = @event.Userid;

                if (player.IsValid && !player.IsBot)
                {
                    Utils.OnPlayerConnect(player);

                    AddTimer(1f, () =>
                    {
                        if (ClientprefsApi.GetPlayerCookie(player, g_iCookieID).Equals("true"))
                        {
                            Globals.playerStats[player.Slot].StrafeStatsEnabled = true;
                        }

                        if (ClientprefsApi.GetPlayerCookie(player, g_iCookieID2).Equals("true"))
                        {
                            Globals.playerStats[player.Slot].StrafeHudEnabled = true;
                        }
                    });
                }
            }
            return HookResult.Continue;
        });
        
        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            if (@event.Userid!.IsValid)
            {
                var player = @event.Userid;

                if (player.IsBot || !player.IsValid)
                    return HookResult.Continue;
                
                Utils.OnPlayerDisconnect(player);
            }
            return HookResult.Continue;
        });
        
        RegisterEventHandler<EventPlayerJump>((@event, info) =>
        {
            if (@event.Userid!.IsValid)
            {
                var player = @event.Userid;

                if (player.IsValid && !player.IsBot)
                    OnPlayerJumped(player);
            }
            return HookResult.Continue;
        });
        
        RegisterListener<Listeners.CheckTransmit>((CCheckTransmitInfoList infoList) =>
        {
            IEnumerable<CCSPlayerController> players = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

            if (!players.Any())
                return;

            foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
            {
                if (player == null || player.IsBot || !player.IsValid || player.IsHLTV)
                    continue;

                if (!Globals.connectedPlayers.TryGetValue(player.Slot, out var connected))
                    continue;

                foreach (var target in Utilities.FindAllEntitiesByDesignerName<CPointWorldText>("point_worldtext"))
                {
                    if (target == null)
                        continue;
                    
                    if (!target.FontName.Equals("Consolas"))
                        continue;

                    if (Globals.playerStats[player.Slot].LeftText is null ||
                        Globals.playerStats[player.Slot].RightText is null ||
                        Globals.playerStats[player.Slot].MouseText is null)
                    {
                        info.TransmitEntities.Remove(target);
                        continue;
                    }

                    if (!target.EntityHandle.Equals(Globals.playerStats[player.Slot].LeftText!.EntityHandle) &&
                        !target.EntityHandle.Equals(Globals.playerStats[player.Slot].RightText!.EntityHandle) &&
                        !target.EntityHandle.Equals(Globals.playerStats[player.Slot].MouseText!.EntityHandle))
                    {
                        info.TransmitEntities.Remove(target);
                    }
                }
            }
        });

        AddCommand("strafestats", "Enable/disable console stats", (player, info) =>
        {
            if (player == null || player.IsBot) return;
            Globals.playerStats[player.Slot].StrafeStatsEnabled = !Globals.playerStats[player.Slot].StrafeStatsEnabled;

            if (Globals.playerStats[player.Slot].StrafeStatsEnabled)
            {
                ClientprefsApi.SetPlayerCookie(player, g_iCookieID, "true");
            }
            else
            {
                ClientprefsApi.SetPlayerCookie(player, g_iCookieID, "false");
            }
            
            player.PrintToChat($"[StrafeHUD] Strafe stats: {(Globals.playerStats[player.Slot].StrafeStatsEnabled ? "Enabled" : "Disabled")}");
        });
        
        AddCommand("strafehud", "Enable/disable hud stats", (player, info) =>
        {
            if (player == null || player.IsBot) return;

            if (!Globals.playerStats[player.Slot].StrafeStatsEnabled)
            {
                player.PrintToChat($"[StrafeHUD] You must enable strafestats first!");
                return;
            }
            Globals.playerStats[player.Slot].StrafeHudEnabled = !Globals.playerStats[player.Slot].StrafeHudEnabled;
            if (Globals.playerStats[player.Slot].StrafeHudEnabled)
            {
                ClientprefsApi.SetPlayerCookie(player, g_iCookieID2, "true");
            }
            else
            {
                ClientprefsApi.SetPlayerCookie(player, g_iCookieID2, "false"); 
            }
            player.PrintToChat($"[StrafeHUD] Strafe hud: {(Globals.playerStats[player.Slot].StrafeHudEnabled ? "Enabled" : "Disabled")}");
        });

        Logger.LogInformation("[StrafeHUD] Loaded!");
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        ClientprefsApi = g_PluginCapability.Get();

        if (ClientprefsApi == null) return;

        ClientprefsApi.OnDatabaseLoaded += OnClientprefDatabaseReady;
    }

    public void OnClientprefDatabaseReady()
    {
        if (ClientprefsApi is null) return;

        g_iCookieID = ClientprefsApi.RegPlayerCookie("strafestats_enabled", "Player has enabled strafestats");
        g_iCookieID2 = ClientprefsApi.RegPlayerCookie("strafehud_enabled", "Player has enabled strafehud");

        if (g_iCookieID == -1)
        {
            Logger.LogError("[StrafeHUD] Failed to register player cookie strafestats_enabled!");
            return;
        }
        
        if (g_iCookieID2 == -1)
        {
            Logger.LogError("[StrafeHUD] Failed to register player cookie strafehud_enabled!");
            return;
        }
    }

    private HookResult OnRunCommand(DynamicHook h)
    {
        var player = h.GetParam<CCSPlayer_MovementServices>(movementServices).Pawn.Value.Controller.Value
            ?.As<CCSPlayerController>();

        if (player == null || player.IsBot || !player.IsValid || player.IsHLTV) return HookResult.Continue;

        var userCmd = new CUserCmd(h.GetParam<IntPtr>(movementPtr));
        var baseCmd = userCmd.GetBaseCmd();
        var getMovementButton = userCmd.GetMovementButton();

        if (player != null && !player.IsBot && player.IsValid && !player.IsHLTV)
        {
            try
            {
                if (!Globals.playerStats[player.Slot].StrafeStatsEnabled) return HookResult.Continue;
                var moveLeft = getMovementButton.Contains("Left");
                var moveRight = getMovementButton.Contains("Right");
                var sideMove = baseCmd.SideMove;
                var forwardMove = baseCmd.ForwardMove;
                
                PlayerFlags flags = (PlayerFlags)player.Pawn.Value!.Flags;
                Globals.playerStats[player.Slot].LastButtons = Globals.playerStats[player.Slot].Buttons;
                Globals.playerStats[player.Slot].Buttons = player.Buttons;
                
                Globals.playerStats[player.Slot].LastFlags = Globals.playerStats[player.Slot].Flags;
                Globals.playerStats[player.Slot].Flags = flags;
                
                Globals.playerStats[player.Slot].LastSideMove = Globals.playerStats[player.Slot].SideMove;
                Globals.playerStats[player.Slot].SideMove = sideMove;
                
                Globals.playerStats[player.Slot].LastForwardMove = Globals.playerStats[player.Slot].ForwardMove;
                Globals.playerStats[player.Slot].ForwardMove = forwardMove;
                
                Globals.playerStats[player.Slot].LastPosition = new Vector(
                    Globals.playerStats[player.Slot].Position.X,
                    Globals.playerStats[player.Slot].Position.Y,
                    Globals.playerStats[player.Slot].Position.Z
                );
                Globals.playerStats[player.Slot].Position = new Vector(
                    player.PlayerPawn.Value!.AbsOrigin!.X,
                    player.PlayerPawn.Value!.AbsOrigin!.Y,
                    player.PlayerPawn.Value!.AbsOrigin!.Z
                );
                
                Globals.playerStats[player.Slot].LastAngles = new QAngle(
                    Globals.playerStats[player.Slot].Angles.X,
                    Globals.playerStats[player.Slot].Angles.Y,
                    Globals.playerStats[player.Slot].Angles.Z
                );
                var viewAngles = userCmd.GetViewAngles()!;
                Globals.playerStats[player.Slot].Angles = new QAngle(
                    viewAngles.X,
                    viewAngles.Y,
                    viewAngles.Z
                );
                
                Globals.playerStats[player.Slot].LastVelocity = new Vector(
                    Globals.playerStats[player.Slot].Velocity.X,
                    Globals.playerStats[player.Slot].Velocity.Y,
                    Globals.playerStats[player.Slot].Velocity.Z
                );
                Globals.playerStats[player.Slot].Velocity = new Vector(
                    player.PlayerPawn.Value.AbsVelocity.X,
                    player.PlayerPawn.Value.AbsVelocity.Y,
                    player.PlayerPawn.Value.AbsVelocity.Z
                );

                if (((PlayerFlags)player.Pawn.Value!.Flags & PlayerFlags.FL_ONGROUND) != 0 || (player.PlayerPawn.Value.GroundEntity != null && player.PlayerPawn.Value.GroundEntity.IsValid && player.PlayerPawn.Value.GroundEntity.Index != 0))
                {
                    Globals.playerStats[player.Slot].FramesInAir = 0;
                    Globals.playerStats[player.Slot].FramesOnGround++;
                }
                else
                {
                    Globals.playerStats[player.Slot].FramesInAir++;
                    Globals.playerStats[player.Slot].FramesOnGround = 0;
                }

                Globals.playerStats[player.Slot].LastStamina = Globals.playerStats[player.Slot].Stamina;
                Globals.playerStats[player.Slot].Stamina = Globals.playerStats[player.Slot].MovementService!.Stamina;
                
                // lj stuff
                if (Globals.playerStats[player.Slot].FramesInAir == 1)
                {
                    if (Globals.playerStats[player.Slot].LastGroundPosition !=
                        Globals.playerStats[player.Slot].LastPosition)
                    {
                        Globals.playerStats[player.Slot].LastGroundPosition = Globals.playerStats[player.Slot].LastPosition;
                        // WalkedOff = true
                    }
                }

                bool forwardPressed = getMovementButton.Contains("Forward");
                
                if (Globals.playerStats[player.Slot].LastForwardPressed && !forwardPressed)
                {
                    Globals.playerStats[player.Slot].ForwardReleaseFrame = Globals.playerStats[player.Slot].TickCount;
                }
                Globals.playerStats[player.Slot].LastForwardPressed = forwardPressed;
                
                if (Globals.playerStats[player.Slot].FramesOnGround == 1)
                {
                    //TrackJump(player, moveLeft, moveRight);
                    OnPlayerLanded(player);
                }

                if (Globals.playerStats[player.Slot].TrackingJump)
                {
                    TrackJump(player, moveLeft, moveRight);
                }

                Globals.playerStats[player.Slot].TickCount++;
                
                return HookResult.Changed;
            }
            catch (Exception ex)
            {
                return HookResult.Continue;
            }
        }
        return HookResult.Continue;
    }

    public void TrackJump(CCSPlayerController? player, bool moveLeft, bool moveRight)
    {
        if (Globals.playerStats[player!.Slot].FramesOnGround > 16)
        {
            Utils.ResetJump(player);
        }
        
        Globals.playerStats[player.Slot].JumpAirtime++;

        float speed = player.PlayerPawn.Value!.AbsVelocity.Length2D();
        if (speed > Globals.playerStats[player.Slot].JumpMaxspeed)
        {
            Globals.playerStats[player.Slot].JumpMaxspeed = speed;
        }

        float lastSpeed = Globals.playerStats[player!.Slot].LastVelocity.Length2D();
        if (speed > lastSpeed)
        {
            Globals.playerStats[player!.Slot].JumpSync++;
        }
        else if (speed < lastSpeed)
        {
            // SPEED LOSS
        }
        float height = Globals.playerStats[player!.Slot].Position.Z - Globals.playerStats[player!.Slot].JumpPosition.Z;
        if (height > Globals.playerStats[player!.Slot].JumpHeight)
        {
            Globals.playerStats[player!.Slot].JumpHeight = height;
        }

        if (Utils.IsOverlapping(Globals.playerStats[player.Slot].Buttons))
        {
            Globals.playerStats[player.Slot].JumpOverlap++;
        }

        if (Utils.IsDeadAirtime(Globals.playerStats[player.Slot].Buttons))
        {
            Globals.playerStats[player.Slot].JumpDeadair++;
        }

        // strafe stats!
        if (Globals.playerStats[player!.Slot].StrafeCount + 1 < 32)
        {
            if (Utils.IsNewStrafe(player))
            {
                if (Globals.playerStats[player.Slot].StrafeCount > 0)
                {
                    Globals.playerStats[player.Slot].StrafeWidths[Globals.playerStats[player.Slot].StrafeCount-1] = 
                        Utils.GetStrafeWidth(Globals.playerStats[player.Slot].StrafeAngle, Globals.playerStats[player.Slot].Angles);
                }
                Globals.playerStats[player.Slot].StrafeCount++;
                Globals.playerStats[player.Slot].LastStrafeAngle = Globals.playerStats[player.Slot].StrafeAngle;
                Globals.playerStats[player.Slot].StrafeAngle = Globals.playerStats[player.Slot].Angles;
            }

            int strafe = Globals.playerStats[player.Slot].StrafeCount;
            Globals.playerStats[player.Slot].StrafeAirtime[strafe]++;

            if (speed > lastSpeed)
            {
                Globals.playerStats[player!.Slot].StrafeSync[strafe] += 1;
                Globals.playerStats[player!.Slot].StrafeGain[strafe] += speed - lastSpeed;
            }
            else if (speed < lastSpeed)
            {
                Globals.playerStats[player!.Slot].StrafeLoss[strafe] += lastSpeed - speed;
            }

            if (speed > Globals.playerStats[player!.Slot].StrafeMax[strafe])
            {
                Globals.playerStats[player!.Slot].StrafeMax[strafe] = speed;
            }

            if (Utils.IsOverlapping(Globals.playerStats[player.Slot].Buttons))
            {
                Globals.playerStats[player.Slot].StrafeOverlap[strafe]++;
            }

            if (Utils.IsDeadAirtime(Globals.playerStats[player.Slot].Buttons))
            {
                Globals.playerStats[player.Slot].StrafeDeadair[strafe]++;
            }

            // efficiency!

            // strafe type & mouse graph
            if (Globals.playerStats[player!.Slot].JumpAirtime - 1 < 150)
            {
                StrafeType strafeType = StrafeType.NONE;

                bool velLeft = Utils.IsWishspeedMovingLeft(Globals.playerStats[player!.Slot].SideMove);
                bool velRight = Utils.IsWishspeedMovingRight(Globals.playerStats[player!.Slot].SideMove);
                bool velIsZero = !velLeft && !velRight;

                if (moveLeft && !moveRight && (velLeft || velRight))
                {
                    strafeType = StrafeType.LEFT;
                }
                else if (moveRight && !moveLeft && (velLeft || velRight))
                {
                    strafeType = StrafeType.RIGHT;
                }
                else if (moveRight && moveLeft && velIsZero)
                {
                    strafeType = StrafeType.OVERLAP;
                }
                else if (moveRight && moveLeft && velLeft)
                {
                    strafeType = StrafeType.OVERLAP_LEFT;
                }
                else if (moveRight && moveLeft && velRight)
                {
                    strafeType = StrafeType.OVERLAP_RIGHT;
                }
                else if (!moveRight && !moveLeft && velIsZero)
                {
                    strafeType = StrafeType.NONE;
                }
                else if (!moveRight && !moveLeft && velLeft)
                {
                    strafeType = StrafeType.NONE_LEFT;
                }
                else if (!moveRight && !moveLeft && velRight)
                {
                    strafeType = StrafeType.NONE_RIGHT;
                }

                Globals.playerStats[player!.Slot].StrafeGraph[Globals.playerStats[player!.Slot].JumpAirtime - 1] =
                    strafeType;
                float yawDiff = Utils.NormalizeYaw(Globals.playerStats[player.Slot].Angles.Y -
                                                   Globals.playerStats[player.Slot].LastAngles.Y);
                int yawIndex = Utils.IntMax(Globals.playerStats[player.Slot].JumpAirtime - 2, (int)0);
                Globals.playerStats[player!.Slot].MouseGraph[yawIndex] = yawDiff;
            }
            
            if (Globals.playerStats[player.Slot].FramesOnGround == 0)
            {
                Vector delta = Utils.SubtractVectors(Globals.playerStats[player.Slot].Position,
                    Globals.playerStats[player.Slot].LastPosition);
                Globals.playerStats[player.Slot].JumpAirpath += delta.Length2D();
            }
        }
    }

    public void OnPlayerJumped(CCSPlayerController? player)
    {
        if (Globals.playerStats[player!.Slot].LeftText is not null && Globals.playerStats[player.Slot].RightText is not null && Globals.playerStats[player.Slot].MouseText is not null)
        {
            Globals.playerStats[player.Slot].LeftText!.AcceptInput("SetMessage", Globals.playerStats[player.Slot].LeftText, Globals.playerStats[player.Slot].LeftText);
            Globals.playerStats[player.Slot].RightText!.AcceptInput("SetMessage", Globals.playerStats[player.Slot].RightText, Globals.playerStats[player.Slot].RightText);
            Globals.playerStats[player.Slot].MouseText!.AcceptInput("SetMessage", Globals.playerStats[player.Slot].MouseText, Globals.playerStats[player.Slot].MouseText);
        }
        Utils.ResetJump(player);
        Globals.playerStats[player!.Slot].TrackingJump = true;
        Globals.playerStats[player!.Slot].JumpFrame = Globals.playerStats[player!.Slot].TickCount;
        if (Globals.playerStats[player.Slot].ForwardReleaseFrame > Globals.playerStats[player.Slot].JumpFrame)
        {
            Globals.playerStats[player.Slot].JumpForwardRelease = 
                Globals.playerStats[player.Slot].ForwardReleaseFrame - Globals.playerStats[player.Slot].JumpFrame;
        }
        else 
        {
            Globals.playerStats[player.Slot].JumpForwardRelease = 0;
        }
        Globals.playerStats[player!.Slot].JumpPosition = new Vector(
            Globals.playerStats[player!.Slot].Position.X,
            Globals.playerStats[player!.Slot].Position.Y,
            Globals.playerStats[player!.Slot].Position.Z
        );
        Globals.playerStats[player!.Slot].JumpAngles = Globals.playerStats[player!.Slot].Angles;

        Globals.playerStats[player!.Slot].JumpPrespeed = player.PlayerPawn.Value!.AbsVelocity.Length2D();
        Globals.playerStats[player!.Slot].JumpGroundZ = Globals.playerStats[player!.Slot].JumpPosition.Z;
    }

    public void OnPlayerLanded(CCSPlayerController? player)
    {
        Globals.playerStats[player!.Slot].LandedDucked = ((Globals.playerStats[player.Slot].Flags & PlayerFlags.FL_DUCKING) != 0);
        if (!Globals.playerStats[player.Slot].TrackingJump)
        {
            Utils.ResetJump(player);
            return;
        }

        float roughOffset = Globals.playerStats[player.Slot].Position.Z -
                            Globals.playerStats[player.Slot].JumpPosition.Z;
        if (roughOffset > 2.0)
        {
            Utils.ResetJump(player);
            return;
        }
        
        if (Globals.playerStats[player.Slot].StrafeCount > 0)
        {
            Globals.playerStats[player.Slot].StrafeWidths[Globals.playerStats[player.Slot].StrafeCount-1] = 
                Utils.GetStrafeWidth(Globals.playerStats[player.Slot].StrafeAngle, Globals.playerStats[player.Slot].Angles);
        }

        Vector landGround = Utils.TraceGround(Globals.playerStats[player.Slot].Position);
        Globals.playerStats[player.Slot].LandGroundZ = landGround.Z;
        
        float gravity = 800f;
        float frametime = 0.015625f;
        Vector fixedVelocity;
        Vector airOrigin;

        var lastPosition = Globals.playerStats[player.Slot].LastPosition;
        bool lastDucking = (Globals.playerStats[player.Slot].LastFlags & PlayerFlags.FL_DUCKING) != PlayerFlags.FL_DUCKING;
        bool ducking = (Globals.playerStats[player.Slot].Flags & PlayerFlags.FL_DUCKING) != PlayerFlags.FL_DUCKING;
        if (!lastDucking && ducking)
        {
            lastPosition.Z += 9;
        }
        else if (lastDucking && !ducking)
        {
            lastPosition.Z -= 9;
        }
        
        bool isBugged = Globals.playerStats[player.Slot].LastPosition.Z - Globals.playerStats[player.Slot].LandGroundZ <
                        2.0f;
        if (isBugged)
        {
            fixedVelocity = Globals.playerStats[player.Slot].Velocity;
            fixedVelocity.Z = Globals.playerStats[player.Slot].LastVelocity.Z - gravity * 0.5f * frametime;
            airOrigin = lastPosition;
        }
        else
        {
            var tempVel = Globals.playerStats[player.Slot].Velocity;
            tempVel.Z = Globals.playerStats[player.Slot].LastVelocity.Z - gravity * 0.5f * frametime;
            fixedVelocity = tempVel;
            fixedVelocity.Z -= gravity * frametime;

            airOrigin = Globals.playerStats[player.Slot].Position;
        }
        var landOrigin = Utils.GetRealLandingOrigin(Globals.playerStats[player.Slot].LandGroundZ, airOrigin, fixedVelocity);
        Globals.playerStats[player.Slot].LandPosition = landOrigin;
        
        Globals.playerStats[player.Slot].JumpDistance = (float)Utils.GetVectorDistance2D(
            Globals.playerStats[player.Slot].JumpPosition, Globals.playerStats[player.Slot].LandPosition);
        Globals.playerStats[player.Slot].JumpDistance += 32;
        
        FinishTrackingJump(player);
        PrintStats(player);

        Utils.ResetJump(player);
    }

    public void FinishTrackingJump(CCSPlayerController? player)
    {
        float xAxisVeer = Math.Abs(Globals.playerStats[player!.Slot].LandPosition.X -
                                   Globals.playerStats[player.Slot].JumpPosition.X);
        float yAxisVeer = Math.Abs(Globals.playerStats[player.Slot].LandPosition.Y -
                                   Globals.playerStats[player.Slot].JumpPosition.Y);
        Globals.playerStats[player.Slot].JumpVeer = Math.Min(xAxisVeer, yAxisVeer);

        Globals.playerStats[player.Slot].JumpForwardRelease = Globals.playerStats[player.Slot].ForwardReleaseFrame -
                                                              Globals.playerStats[player.Slot].JumpFrame;
        Globals.playerStats[player.Slot].JumpSync = (Globals.playerStats[player.Slot].JumpSync /
            Globals.playerStats[player.Slot].JumpAirtime * 100f);

        for (int strafe = 0; strafe < Globals.playerStats[player.Slot].StrafeCount; strafe++)
        {
            Globals.playerStats[player.Slot].StrafeAvgGain[strafe] =
                Globals.playerStats[player.Slot].StrafeGain[strafe] /
                Globals.playerStats[player.Slot].StrafeAirtime[strafe];

            try
            {
                Globals.playerStats[player.Slot].StrafeAvgEfficiency[strafe] /=
                    Globals.playerStats[player.Slot].StrafeAvgEfficiencyCount[strafe];
            }
            catch
            {
                Globals.playerStats[player.Slot].StrafeAvgEfficiency[strafe] = 0;
            }

            if (Globals.playerStats[player.Slot].StrafeAirtime[strafe] != 0)
            {
                Globals.playerStats[player.Slot].StrafeSync[strafe] =
                    (Globals.playerStats[player.Slot].StrafeSync[strafe] /
                     Globals.playerStats[player.Slot].StrafeAirtime[strafe]) * 100;
            }
            else
            {
                Globals.playerStats[player.Slot].StrafeSync[strafe] = 0;
            }
        }

        Vector delta = Utils.SubtractVectors(Globals.playerStats[player.Slot].LandPosition,
            Globals.playerStats[player.Slot].LastPosition);
        Globals.playerStats[player.Slot].JumpAirpath += delta.Length2D();
        Globals.playerStats[player.Slot].JumpAirpath /= (Globals.playerStats[player.Slot].JumpDistance - 32);

        Globals.playerStats[player.Slot].JumpBlockDistance = -1;
        Globals.playerStats[player.Slot].JumpLandEdge = -9999.9f;
        Globals.playerStats[player.Slot].JumpEdge = -1;

        int blockAxis = Math.Abs(Globals.playerStats[player.Slot].LandPosition.Y -
                                 Globals.playerStats[player.Slot].JumpPosition.Y) >
                        Math.Abs(Globals.playerStats[player.Slot].LandPosition.X -
                                 Globals.playerStats[player.Slot].JumpPosition.X)
            ? 1
            : 0;
        int blockDir = (int)Utils.FloatSign(Globals.playerStats[player.Slot].JumpPosition[blockAxis] -
                                            Globals.playerStats[player.Slot].LandPosition[blockAxis]);

        Vector jumpOrigin = Globals.playerStats[player.Slot].JumpPosition;
        Vector landOrigin = Globals.playerStats[player.Slot].LandPosition;

        jumpOrigin.Z -= 2;
        landOrigin.Z -= 2;

        landOrigin[blockAxis] -= blockDir * 16;

        var tempPos = landOrigin;
        tempPos[blockAxis] += (jumpOrigin[blockAxis] - landOrigin[blockAxis]) / 2;

        Vector jumpEdge = Utils.TraceBlock(tempPos, jumpOrigin);

        tempPos = jumpOrigin;
        tempPos[blockAxis] += (landOrigin[blockAxis] - jumpOrigin[blockAxis]) / 2;

        Vector landEdge = Utils.TraceBlock(tempPos, landOrigin);

        if (landEdge != Vector.Zero)
        {
            Globals.playerStats[player.Slot].JumpBlockDistance =
                Math.Abs(landEdge[blockAxis] - jumpEdge[blockAxis]) + 32;
            Globals.playerStats[player.Slot].JumpLandEdge =
                (landEdge[blockAxis] - Globals.playerStats[player.Slot].LandPosition[blockAxis]) * blockDir;
        }

        if (jumpEdge[blockAxis] - tempPos[blockAxis] != 0)
        {
            Globals.playerStats[player.Slot].JumpEdge = Math.Abs(jumpOrigin[blockAxis] - jumpEdge[blockAxis]);
        }

        // jumpoff angles
        Vector airpathDir = Utils.SubtractVectors(Globals.playerStats[player.Slot].LandPosition,
            Globals.playerStats[player.Slot].JumpPosition);
        airpathDir = Utils.NormalizeVector(airpathDir);

        Vector airpathAngles = Utils.GetVectorAngles(airpathDir);
        float airpathYaw = Utils.NormalizeYaw(airpathAngles.Y);

        Globals.playerStats[player.Slot].JumpoffAngle =
            Utils.NormalizeYaw(airpathYaw - Globals.playerStats[player.Slot].JumpAngles.Y);
    }

    public void PrintStats(CCSPlayerController? player)
    {
        // beam stuff in future
        string fwdRelease = "";
        if (Globals.playerStats[player!.Slot].JumpForwardRelease == 0)
        {
            fwdRelease = "Fwd: 0";
        }
        else if (Math.Abs(Globals.playerStats[player.Slot].JumpForwardRelease) > 16)
        {
            fwdRelease = "Fwd: No";
        }
        else if (Globals.playerStats[player.Slot].JumpForwardRelease > 0)
        {
            fwdRelease = $"Fwd: {Globals.playerStats[player.Slot].JumpForwardRelease}";
        }
        else
        {
            fwdRelease = $"Fwd: {Globals.playerStats[player.Slot].JumpForwardRelease}";
        }

        string edge = "";
        bool hasEdge = false;
        if (Globals.playerStats[player.Slot].JumpEdge >= 0 && Globals.playerStats[player.Slot].JumpEdge < 32)
        {
            edge = $"Edge: {Globals.playerStats[player.Slot].JumpEdge}";
            hasEdge = true;
        }

        string block = "";
        bool hasBlock = false;
        if (Globals.playerStats[player.Slot].JumpBlockDistance > 200)
        {
            block = $"Block: {(double)Globals.playerStats[player.Slot].JumpBlockDistance}";
            hasBlock = true;
        }

        string landEdge = "";
        bool hasLandEdge = false;
        if (Math.Abs(Globals.playerStats[player.Slot].JumpLandEdge) < 32)
        {
            landEdge = $"Land Edge: {Globals.playerStats[player.Slot].JumpLandEdge}";
            hasLandEdge = true;
        }

        string fog = "";
        bool hasFog = false;
        if (Globals.playerStats[player.Slot].PrespeedFog <= 8 && Globals.playerStats[player.Slot].PrespeedFog >= 0)
        {
            fog = $"Fog: {Globals.playerStats[player.Slot].PrespeedFog}";
            hasFog = true;
        }

        string stamina = "";
        bool hasStamina = false;
        if (Globals.playerStats[player.Slot].PrespeedStamina != 0)
        {
            stamina = $"Stamina: {Globals.playerStats[player.Slot].PrespeedStamina}";
            hasStamina = true;
        }

        string offset = "";
        bool hasOffset = false;
        if (Globals.playerStats[player.Slot].JumpGroundZ != Globals.playerStats[player.Slot].JumpPosition.Z)
        {
            offset =
                $"Ground Z: {Globals.playerStats[player.Slot].JumpPosition.Z - Globals.playerStats[player.Slot].JumpGroundZ}";
            hasOffset = true;
        }

        // printing
        string consoleStats =
            $"\n[GCRC] {(Globals.playerStats[player.Slot].FailedJump ? "FAILED " : "")}LJ: {Globals.playerStats[player.Slot].JumpDistance} [{block}{(hasBlock ? " | " : "")}{edge}{(hasEdge ? " | " : "")}Veer: {Globals.playerStats[player.Slot].JumpVeer} | {fwdRelease} | Sync: {Globals.playerStats[player.Slot].JumpSync} | Max: {Globals.playerStats[player.Slot].JumpMaxspeed}]\n" +
            $"[{landEdge}{(hasLandEdge ? " | " : "")}Pre: {Globals.playerStats[player.Slot].JumpPrespeed} | OL/DA: {Globals.playerStats[player.Slot].JumpOverlap}/{Globals.playerStats[player.Slot].JumpDeadair} | Jumpoff Angle: {Globals.playerStats[player.Slot].JumpoffAngle} | Airpath: {Globals.playerStats[player.Slot].JumpAirpath}]\n" +
            $"[Strafes: {Globals.playerStats[player.Slot].StrafeCount + 1} | Airtime: {Globals.playerStats[player.Slot].JumpAirtime} | {fog}{(hasFog ? " | " : "")}Height: {Globals.playerStats[player.Slot].JumpHeight}{(hasOffset ? " | " : "")}{offset}{(hasStamina ? " | " : "")}{stamina}]";
        player.PrintToConsole(consoleStats);

        player.PrintToConsole(" #.  Sync    Gain   Loss   Max  Air  OL  DA  AvgGain  Width");
        for (int strafe = 0; strafe <= Globals.playerStats[player.Slot].StrafeCount && strafe < 32; strafe++)
        {
            player.PrintToConsole($"{strafe + 1}. " +
                                  $"{Globals.playerStats[player.Slot].StrafeSync[strafe],5:F1}% " +
                                  $"{Globals.playerStats[player.Slot].StrafeGain[strafe],6:F2} " +
                                  $"{Globals.playerStats[player.Slot].StrafeLoss[strafe],6:F2}  " +
                                  $"{Globals.playerStats[player.Slot].StrafeMax[strafe],5:F1} " + 
                                  $"{Globals.playerStats[player.Slot].StrafeAirtime[strafe],3} " + 
                                  $"{Globals.playerStats[player.Slot].StrafeOverlap[strafe],3} " + 
                                  $"{Globals.playerStats[player.Slot].StrafeDeadair[strafe],3}   " +
                                  $"{Globals.playerStats[player.Slot].StrafeAvgGain[strafe],3:F2}     " + 
                                  $"{Globals.playerStats[player.Slot].StrafeWidths[strafe]}\u00b0");
        }

        int slIndex = 0;
        int srIndex = 0;
        int mlIndex = 0;
        int mrIndex = 0;

        StringBuilder strafeLeftBuilder = new StringBuilder();
        StringBuilder strafeRightBuilder = new StringBuilder();
        StringBuilder mouseLeftBuilder = new StringBuilder();
        StringBuilder mouseRightBuilder = new StringBuilder();

        StringBuilder hudStrafeLeftBuilder = new StringBuilder();
        int hslIndex = 0;
        StringBuilder hudStrafeRightBuilder = new StringBuilder();
        int hsrIndex = 0;
        StringBuilder hudMouseBuilder = new StringBuilder();
        int hmIndex = 0;

        StrafeType lastStrafeTypeLeft = (StrafeType)420;
        StrafeType lastStrafeTypeRight = (StrafeType)420;
        int lastMouseIndex = 9999;
        for (int i = 0; i < Globals.playerStats[player.Slot].JumpAirtime && i < 150; i++)
        {
            StrafeType strafeType = Globals.playerStats[player.Slot].StrafeGraph[i];

            StrafeType strafeTypeLeft = strafeType;
            if (strafeTypeLeft == StrafeType.RIGHT ||
                strafeTypeLeft == StrafeType.NONE_RIGHT ||
                strafeTypeLeft == StrafeType.OVERLAP_RIGHT)
            {
                strafeTypeLeft = StrafeType.NONE;
            }

            StrafeType strafeTypeRight = strafeType;
            if (strafeTypeRight == StrafeType.LEFT ||
                strafeTypeRight == StrafeType.NONE_LEFT ||
                strafeTypeRight == StrafeType.OVERLAP_LEFT)
            {
                strafeTypeRight = StrafeType.NONE;
            }
            else if (strafeTypeRight == StrafeType.RIGHT)
            {
                strafeTypeRight = StrafeType.RIGHT;
            }

            strafeLeftBuilder.Append(StrafeChars[(int)strafeTypeLeft]);
            slIndex++;

            strafeRightBuilder.Append(StrafeChars[(int)strafeTypeRight]);
            srIndex++;

            string mouseChar = "|";
            if (Globals.playerStats[player.Slot].MouseGraph[i] == 0)
            {
                mouseLeftBuilder.Append(".");
                mouseRightBuilder.Append(".");
            }
            else if (Globals.playerStats[player.Slot].MouseGraph[i] < 0)
            {
                mouseLeftBuilder.Append(".");
                mouseRightBuilder.Append(mouseChar);
                mrIndex++;
            }
            else if (Globals.playerStats[player.Slot].MouseGraph[i] > 0)
            {
                mouseLeftBuilder.Append(mouseChar);
                mouseRightBuilder.Append(".");
                mlIndex++;
            }

            if (i == 0)
            {
                hudStrafeLeftBuilder.Append("L:");
                hudStrafeRightBuilder.Append("R:");
                hudMouseBuilder.Append("M:");
            }

            if (lastStrafeTypeLeft != strafeTypeLeft)
            {
                hudStrafeLeftBuilder.Append(HudChars[(int)strafeTypeLeft]);
            }
            else
            {
                hudStrafeLeftBuilder.Append(HudChars[(int)strafeTypeLeft]);
            }

            if (lastStrafeTypeRight != strafeTypeRight)
            {
                hudStrafeRightBuilder.Append(HudChars[(int)strafeTypeRight]);
            }
            else
            {
                hudStrafeRightBuilder.Append(HudChars[(int)strafeTypeRight]);
            }

            int mouseIndex = (int)Utils.FloatSign(Globals.playerStats[player.Slot].MouseGraph[i]) + 1;
            if (mouseIndex != lastMouseIndex)
            {
                hudMouseBuilder.Append(".");
            }
            else
            {
                hudMouseBuilder.Append("|");
            }

            lastStrafeTypeLeft = strafeTypeLeft;
            lastStrafeTypeRight = strafeTypeRight;
            lastMouseIndex = mouseIndex;

            bool isLastTick = (i == Math.Min(Globals.playerStats[player.Slot].JumpAirtime - 1, 149));
            if (isLastTick)
            {
                if (Globals.playerStats[player.Slot].StrafeHudEnabled)
                {
                    Globals.playerStats[player.Slot].LeftText = Utils.CreateLeftStrafeHud(player, hudStrafeLeftBuilder.ToString(), 30, Color.Black, "Consolas");
                    Globals.playerStats[player.Slot].RightText = Utils.CreateRightStrafeHud(player, hudStrafeRightBuilder.ToString(), 30, Color.Black, "Consolas");
                    Globals.playerStats[player.Slot].MouseText = Utils.CreateMouseHud(player, hudMouseBuilder.ToString(), 30, Color.Black, "Consolas");
                }
                player.PrintToConsole($"\nStrafe movement:\nL: {strafeLeftBuilder}\nR: {strafeRightBuilder}" +
                                      $"\nMouse movement:\nL: {mouseLeftBuilder}\nR: {mouseRightBuilder}");
            }
        }
    }

    public enum StrafeType
    {
        OVERLAP,           // A + D are pressed and sidemove is 0
        NONE,              // A + D are not pressed and sidemove is 0
    
        LEFT,              // only A is pressed and sidemove isnt 0
        OVERLAP_LEFT,      // A + D are pressed, but sidemove is smaller than 0
        NONE_LEFT,         // A + D are not pressed and sidemove is smaller than 0
    
        RIGHT,             // only D is pressed and sidemove isnt 0
        OVERLAP_RIGHT,     // A + D are pressed, but sidemove is more than 0
        NONE_RIGHT         // A + D are not pressed and sidemove is more than 0
    }
    
    public string[] StrafeChars =
    [
        "$", // STRAFETYPE_OVERLAP
        ".", // STRAFETYPE_NONE
    
        "|", // STRAFETYPE_LEFT
        "#", // STRAFETYPE_OVERLAP_LEFT
        "H", // STRAFETYPE_NONE_LEFT
    
        "|", // STRAFETYPE_RIGHT
        "#", // STRAFETYPE_OVERLAP_RIGHT
        "H"  // STRAFETYPE_NONE_RIGHT
    ];
    
    public string[] HudChars =
    [
        ":", // STRAFETYPE_OVERLAP
        ".", // STRAFETYPE_NONE

        "|", // STRAFETYPE_LEFT
        ";", // STRAFETYPE_OVERLAP_LEFT
        "!", // STRAFETYPE_NONE_LEFT

        "|", // STRAFETYPE_RIGHT
        ";", // STRAFETYPE_OVERLAP_RIGHT
        "!"  // STRAFETYPE_NONE_RIGHT
    ];
    
    public override void Unload(bool hotReload)
    {
        RunCommand.Unhook(OnRunCommand, HookMode.Pre);

        if (ClientprefsApi is null) return;

        ClientprefsApi.OnDatabaseLoaded -= OnClientprefDatabaseReady;

        Logger.LogInformation("[StrafeHUD] Plugin unloaded.");
    }
}