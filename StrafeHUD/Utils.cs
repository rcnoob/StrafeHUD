using System.Drawing;
using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using StrafeHUD.Extensions;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace StrafeHUD;

public class Utils
{
    public static void OnPlayerConnect(CCSPlayerController? player)
    {
        if (player == null || player.IsBot)
            return;

        Globals.playerStats[player.Slot] = new PlayerStats();
        Globals.playerStats[player.Slot].MovementService =
            new CCSPlayer_MovementServices(player.PlayerPawn.Value!.MovementServices!.Handle);

        Globals.connectedPlayers[player.Slot] = new CCSPlayerController(player.Handle);
    }

    public static void OnPlayerDisconnect(CCSPlayerController? player)
    {
        if (!Globals.connectedPlayers.ContainsKey(player.Slot))
            return;
        
        if (Globals.playerStats.TryGetValue(player.Slot, out var playerStat))
        {
            playerStat.LeftText?.Remove();
            playerStat.RightText?.Remove(); 
            playerStat.MouseText?.Remove();
        }

        Globals.playerStats[player.Slot] = new PlayerStats();
        Globals.playerStats.Remove(player.Slot);
        Globals.connectedPlayers.Remove(player.Slot);
    }

    public static bool IsOverlapping(bool moveLeft, bool moveRight)
    {
        return moveLeft && moveRight;
    }

    public static bool IsDeadAirtime(bool moveLeft, bool moveRight)
    {
        return !moveLeft && !moveRight;
    }

    public static bool IsWishspeedMovingLeft(float sidemove)
    {
        return sidemove < 0;
    }

    public static bool IsWishspeedMovingRight(float sidemove)
    {
        return sidemove > 0;
    }

    public static void ResetJump(CCSPlayerController? player)
    {
        Globals.playerStats[player!.Slot].JumpPosition = new();
        Globals.playerStats[player.Slot].LandPosition = new();
        Globals.playerStats[player.Slot].TrackingJump = false;
        Globals.playerStats[player.Slot].FailedJump = false;

        Globals.playerStats[player.Slot].JumpMaxspeed = 0;
        Globals.playerStats[player.Slot].JumpSync = 0;
        Globals.playerStats[player.Slot].JumpEdge = 0;
        Globals.playerStats[player.Slot].JumpHeight = 0;
        Globals.playerStats[player.Slot].JumpAirtime = 0;
        Globals.playerStats[player.Slot].JumpOverlap = 0;
        Globals.playerStats[player.Slot].JumpDeadair = 0;
        Globals.playerStats[player.Slot].JumpAirpath = 0;
        Globals.playerStats[player.Slot].JumpDistance = 0;

        Globals.playerStats[player.Slot].StrafeCount = 0;
        for (int i = 0; i < 32; i++)
        {
            Globals.playerStats[player.Slot].StrafeSync[i] = 0;
            Globals.playerStats[player.Slot].StrafeGain[i] = 0;
            Globals.playerStats[player.Slot].StrafeLoss[i] = 0;
            Globals.playerStats[player.Slot].StrafeMax[i] = 0;
            Globals.playerStats[player.Slot].StrafeAirtime[i] = 0;
            Globals.playerStats[player.Slot].StrafeOverlap[i] = 0;
            Globals.playerStats[player.Slot].StrafeDeadair[i] = 0;
            Globals.playerStats[player.Slot].StrafeAvgGain[i] = 0;
            Globals.playerStats[player.Slot].StrafeAvgEfficiency[i] = 0;
            Globals.playerStats[player.Slot].StrafeAvgEfficiencyCount[i] = 0;
            Globals.playerStats[player.Slot].StrafeMaxEfficiency[i] = 0;
        }
    }

    public static bool IsNewStrafe(CCSPlayerController? player)
    {
        return ((Globals.playerStats[player!.Slot].SideMove > 0 &&
                 Globals.playerStats[player.Slot].LastSideMove <= 0) ||
                (Globals.playerStats[player.Slot].SideMove < 0 &&
                 Globals.playerStats[player.Slot].LastSideMove >= 0)) &&
               Globals.playerStats[player.Slot].JumpAirtime != 1;
    }

    public static bool IsOverlapping(PlayerButtons buttons)
    {
        return (buttons & PlayerButtons.Moveleft) != 0 && (buttons & PlayerButtons.Moveright) != 0;
    }

    public static bool IsDeadAirtime(PlayerButtons buttons)
    {
        return (buttons & PlayerButtons.Moveleft) == 0 && (buttons & PlayerButtons.Moveright) == 0;
    }
    
    public static Vector GetVectorAngles(Vector vector)
    {
        float tmp, yaw, pitch;

        if (vector.Y == 0 && vector.X == 0)
        {
            yaw = 0;
            pitch = vector.Z > 0 ? 270 : 90;
        }
        else
        {
            yaw = (float)(Math.Atan2(vector.Y, vector.X) * 180 / Math.PI);
            if (yaw < 0)
                yaw += 360;

            tmp = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            pitch = (float)(Math.Atan2(-vector.Z, tmp) * 180 / Math.PI);
            if (pitch < 0)
                pitch += 360;
        }

        return new Vector(pitch, yaw, 0);
    }

    public static Vector NormalizeVector(Vector vec)
    {
        float length = (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);

        if (length == 0)
            return Vector.Zero;

        return new Vector(
            vec.X / length,
            vec.Y / length,
            vec.Z / length
        );
    }

    public static Vector TraceGround(Vector startpos)
    {
        Vector endpos = startpos;
        endpos.Z -= 2;
        
        Vector? result = RayTrace.TraceShape(startpos, endpos, false);

        if (result is null || result == Vector.Zero)
            return startpos;

        return result;
    }

    public static Vector TraceBlock(Vector jumpPos, Vector endpos)
    {
       Vector centerStart = new Vector(jumpPos.X, jumpPos.Y, jumpPos.Z - 1);
       Vector centerEnd = new Vector(endpos.X, endpos.Y, endpos.Z - 1);
       Vector? centerResult = RayTrace.TraceShape(centerStart, centerEnd, false);
       
       Vector leftStart = new Vector(jumpPos.X - 16, jumpPos.Y, jumpPos.Z - 1);
       Vector leftEnd = new Vector(endpos.X - 16, endpos.Y, endpos.Z - 1);
       Vector? leftResult = RayTrace.TraceShape(leftStart, leftEnd, false);

       Vector rightStart = new Vector(jumpPos.X + 16, jumpPos.Y, jumpPos.Z - 1);
       Vector rightEnd = new Vector(endpos.X + 16, endpos.Y, endpos.Z - 1);
       Vector? rightResult = RayTrace.TraceShape(rightStart, rightEnd, false);

       Vector frontStart = new Vector(jumpPos.X, jumpPos.Y - 16, jumpPos.Z - 1);
       Vector frontEnd = new Vector(endpos.X, endpos.Y - 16, endpos.Z - 1);
       Vector? frontResult = RayTrace.TraceShape(frontStart, frontEnd, false);

       Vector backStart = new Vector(jumpPos.X, jumpPos.Y + 16, jumpPos.Z - 1);
       Vector backEnd = new Vector(endpos.X, endpos.Y + 16, endpos.Z - 1);
       Vector? backResult = RayTrace.TraceShape(backStart, backEnd, false);

       float closestDist = float.MaxValue;
       Vector closestHit = jumpPos;

       void CheckTrace(Vector? res)
       {
           if (res != null)
           {
               float dist = (float)Math.Sqrt(
                   Math.Pow(res.X - jumpPos.X, 2) + 
                   Math.Pow(res.Y - jumpPos.Y, 2) + 
                   Math.Pow(res.Z - jumpPos.Z, 2));
               
               if (dist < closestDist)
               {
                   closestDist = dist;
                   closestHit = res;
               }
           }
       }

       CheckTrace(centerResult);
       CheckTrace(leftResult);
       CheckTrace(rightResult);
       CheckTrace(frontResult);
       CheckTrace(backResult);

       return closestHit ?? jumpPos;
    }

    public static Vector GetRealLandingOrigin(float landGroundZ, Vector origin, Vector velocity)
    {
        if ((origin.Z - landGroundZ) == 0)
        {
            return origin;
        }

        float frametime = Globals.Frametime;
        float verticalDistance = -velocity.Z * frametime;
        float fraction = (origin.Z - landGroundZ) / verticalDistance;

        Vector addDistance = velocity;
        ScaleVector(addDistance, frametime * fraction);

        return AddVectors(origin, addDistance);
    }

    public static float FloatSign(float value)
    {
        return value >= 0 ? 1.0f : -1.0f;
    }

    public static Vector ScaleVector(Vector vector, float scale)
    {
        return new Vector(
            vector.X * scale,
            vector.Y * scale,
            vector.Z * scale
        );
    }

    public static Vector AddVectors(Vector vec1, Vector vec2)
    {
        return new Vector(
            vec1.X + vec2.X,
            vec1.Y + vec2.Y,
            vec1.Z + vec2.Z
        );
    }

    public static Vector SubtractVectors(Vector vec1, Vector vec2)
    {
        return new Vector(
            vec1.X - vec2.X,
            vec1.Y - vec2.Y,
            vec1.Z - vec2.Z
        );
    }

    public static double GetVectorDistance2D(Vector x, Vector y)
    {
        double deltaX = x.X - y.X;
        double deltaY = x.Y - y.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    public static float NormalizeYaw(float angle)
    {
        if (angle <= -180)
            angle += 360;

        if (angle > 180)
            angle -= 360;

        return angle;
    }

    public static int IntMax(int n1, int n2)
    {
        return n1 > n2 ? n1 : n2;
    }

    public static double GetStrafeWidth(QAngle x, QAngle y)
    {
        double width = Math.Abs(y.Y - x.Y);

        if (width > 180)
        {
            width -= 360;
        }
        return Math.Abs(Math.Round(width, 1));
    }
    
    public static CPointWorldText CreateLeftStrafeHud(CCSPlayerController player, string text, int size = 100, Color? color = null, string font = "")
    {
        CCSPlayerPawn pawn = player?.PlayerPawn.Value!;

        var handle = new CHandle<CCSGOViewModel>((IntPtr)(pawn.ViewModelServices!.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel") + 4));
        if (!handle.IsValid)
        {
            CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
            viewmodel.DispatchSpawn();
            handle.Raw = viewmodel.EntityHandle.Raw;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_pViewModelServices");
        }

        CPointWorldText worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext")!;
        worldText.MessageText = text;
        worldText.Enabled = true;
        worldText.FontSize = size;
        worldText.Fullbright = true;
        worldText.Color = color ?? Color.Aquamarine;
        worldText.WorldUnitsPerPx = 0.01f;
        worldText.FontName = font;
        worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
        worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
        worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

        QAngle eyeAngles = pawn.EyeAngles;
        Vector forward = new(), right = new(), up = new();
        NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

        Vector eyePosition = new();
        eyePosition += forward * 7;
        eyePosition += right * -5;
        eyePosition += up * 5f;
        QAngle angles = new()
        {
            Y = eyeAngles.Y + 270,
            Z = 90 - eyeAngles.X,
            X = 0
        };

        worldText.DispatchSpawn();
        worldText.Teleport(pawn.AbsOrigin! + eyePosition + new Vector(0, 0, pawn.ViewOffset.Z), angles, null);
        worldText.AcceptInput("SetParent", handle.Value, null, "!activator");

        return worldText;
    }
    public static CPointWorldText CreateRightStrafeHud(CCSPlayerController player, string text, int size = 100, Color? color = null, string font = "")
    {
        CCSPlayerPawn pawn = player?.PlayerPawn.Value!;

        var handle = new CHandle<CCSGOViewModel>((IntPtr)(pawn.ViewModelServices!.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel") + 4));
        if (!handle.IsValid)
        {
            CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
            viewmodel.DispatchSpawn();
            handle.Raw = viewmodel.EntityHandle.Raw;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_pViewModelServices");
        }

        CPointWorldText worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext")!;
        worldText.MessageText = text;
        worldText.Enabled = true;
        worldText.FontSize = size;
        worldText.Fullbright = true;
        worldText.Color = color ?? Color.Aquamarine;
        worldText.WorldUnitsPerPx = 0.01f;
        worldText.FontName = font;
        worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
        worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
        worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

        QAngle eyeAngles = pawn.EyeAngles;
        Vector forward = new(), right = new(), up = new();
        NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

        Vector eyePosition = new();
        eyePosition += forward * 7;
        eyePosition += right * -5;
        eyePosition += up * 4.5f;
        QAngle angles = new()
        {
            Y = eyeAngles.Y + 270,
            Z = 90 - eyeAngles.X,
            X = 0
        };

        worldText.DispatchSpawn();
        worldText.Teleport(pawn.AbsOrigin! + eyePosition + new Vector(0, 0, pawn.ViewOffset.Z), angles, null);
        worldText.AcceptInput("SetParent", handle.Value, null, "!activator");
            
        return worldText;
    }
    public static CPointWorldText CreateMouseHud(CCSPlayerController player, string text, int size = 100, Color? color = null, string font = "")
    {
        CCSPlayerPawn pawn = player?.PlayerPawn.Value!;

        var handle = new CHandle<CCSGOViewModel>((IntPtr)(pawn.ViewModelServices!.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel") + 4));
        if (!handle.IsValid)
        {
            CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
            viewmodel.DispatchSpawn();
            handle.Raw = viewmodel.EntityHandle.Raw;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_pViewModelServices");
        }

        CPointWorldText worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext")!;
        worldText.MessageText = text;
        worldText.Enabled = true;
        worldText.FontSize = size;
        worldText.Fullbright = true;
        worldText.Color = color ?? Color.Aquamarine;
        worldText.WorldUnitsPerPx = 0.01f;
        worldText.FontName = font;
        worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
        worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
        worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

        QAngle eyeAngles = pawn.EyeAngles;
        Vector forward = new(), right = new(), up = new();
        NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

        Vector eyePosition = new();
        eyePosition += forward * 7;
        eyePosition += right * -5;
        eyePosition += up * 4f;
        QAngle angles = new()
        {
            Y = eyeAngles.Y + 270,
            Z = 90 - eyeAngles.X,
            X = 0
        };

        worldText.DispatchSpawn();
        worldText.Teleport(pawn.AbsOrigin! + eyePosition + new Vector(0, 0, pawn.ViewOffset.Z), angles, null);
        worldText.AcceptInput("SetParent", handle.Value, null, "!activator");

        return worldText;
    }
}