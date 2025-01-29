using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
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
        if (player == null || player.IsBot)
            return;

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

        // Avoid division by zero
        if (length == 0)
            return Vector.Zero;

        // Divide each component by the length
        return new Vector(
            vec.X / length,
            vec.Y / length,
            vec.Z / length
        );
    }

    public static Vector TraceGround(Vector pos)
    {
        Vector startPos = pos;
        Vector endPos = pos;

        endPos.Z -= 2;

        Vector result = RayTrace.TraceRay(startPos, endPos, Masks.MASK_SOLID);

        return result;
    }

    public static Vector TraceBlock(Vector pos1, Vector pos2)
    {
        Vector startPos = pos1;
        Vector endPos = pos2;

        Vector result = RayTrace.TraceRay(startPos, endPos, Masks.MASK_SOLID);

        return result;
    }

    public static Vector GetRealLandingOrigin(float landGroundZ, Vector origin, Vector velocity)
    {
        if ((origin.Z - landGroundZ) == 0)
        {
            return origin;
        }

        float frametime = Globals.Frametime;
        float verticalDistance = origin.Z - (origin.Z + velocity.Z * frametime);
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
        Vector x2;
        Vector y2;

        x2 = x;
        y2 = y;

        x2.Z = 0;
        y2.Z = 0;

        return Distance(x2, y2);
    }

    public static double Distance(Vector x, Vector y)
    {
        double deltaX = x.X - y.X;
        double deltaY = x.Y - y.Y;
        double deltaZ = x.Z - y.Z;

        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
    }

    public static Vector GetClientMins()
    {
        return new Vector(-16, -16, 0);
    }

    public static Vector GetClientMaxs(CCSPlayerController? player)
    {
        return (Globals.playerStats[player!.Slot].Flags & PlayerFlags.FL_DUCKING) != 0
            ? new Vector(16, 16, 54)
            : new Vector(16, 16, 72);
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

    public bool IsOverlapping()
    {
        return false;
    }
}