using System.Numerics;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace StrafeHUD.Extensions;

public class RayTrace
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate bool TraceShapeDelegate(
        nint GameTraceManager,
        nint vecStart,
        nint vecEnd,
        nint skip,
        ulong mask,
        byte a6,
        GameTrace* pGameTrace
    );

    private static TraceShapeDelegate _traceShape;

    private static nint TraceFunc = NativeAPI.FindSignature(Addresses.ServerPath, Environment.OSVersion.Platform == PlatformID.Unix
        ? "48 B8 ? ? ? ? ? ? ? ? 55 48 89 E5 41 57 41 56 49 89 D6 41 55"
        : "4C 8B DC 49 89 5B ? 49 89 6B ? 49 89 73 ? 57 41 56 41 57 48 81 EC ? ? ? ? 0F 57 C0");

    private static nint GameTraceManager = NativeAPI.FindSignature(Addresses.ServerPath, Environment.OSVersion.Platform == PlatformID.Unix
        ? "48 8D 05 ? ? ? ? F3 0F 58 8D ? ? ? ? 31 FF"
        : "48 8B 0D ? ? ? ? 48 8D 45 ? 48 89 44 24 ? 4C 8D 44 24 ? C7 44 24 ? ? ? ? ? 48 8D 54 24 ? 4C 8B CB");

        public static unsafe CounterStrikeSharp.API.Modules.Utils.Vector? TraceRay(

        Vector origin, 
        Vector endorigin,
        ulong mask
    ) {
  
        Vector _origin = new (0,0,0);
        Vector _endorigin = new (0,0,0);

        var _gameTraceManagerAddress = Address.GetAbsoluteAddress(GameTraceManager, 3, 7);
        if (_gameTraceManagerAddress is 0) return null;
        
        _traceShape = Marshal.GetDelegateForFunctionPointer<TraceShapeDelegate>(TraceFunc);
        if (_traceShape is null) return null;

        var _forward = new Vector();

        
        _origin = origin;
        _endorigin = endorigin;
        
        var _trace = stackalloc GameTrace[1];

        var result = _traceShape(*(nint*)_gameTraceManagerAddress, _origin.Handle, _endorigin.Handle, 0, mask, 4, _trace);

        if (result is false)
            return new Vector(_trace->EndPos.X, _trace->EndPos.Y, _trace->EndPos.Z);
        
        return null;
    }
}
    
internal static class Address
{
    static unsafe public nint GetAbsoluteAddress(nint addr, nint offset, int size)
    {
        int code = *(int*)(addr + offset);
        return addr + code + size;
    }

    static public nint GetCallAddress(nint a)
    {
        return GetAbsoluteAddress(a, 1, 5);
    }
}
 
[StructLayout(LayoutKind.Explicit, Size = 0x35)]
public unsafe struct Ray
{
    [FieldOffset(0)] public Vector3 Start;
    [FieldOffset(0xC)] public Vector3 End;
    [FieldOffset(0x18)] public Vector3 Mins;
    [FieldOffset(0x24)] public Vector3 Maxs;
    [FieldOffset(0x34)] public byte UnkType;
}

[StructLayout(LayoutKind.Explicit, Size = 0x44)]
public unsafe struct TraceHitboxData
{
    [FieldOffset(0x38)] public int HitGroup;
    [FieldOffset(0x40)] public int HitboxId;
}

[StructLayout(LayoutKind.Explicit, Size = 0xB8)]
public unsafe struct GameTrace
{
    [FieldOffset(0)] public void* Surface;
    [FieldOffset(0x8)] public void* HitEntity;
    [FieldOffset(0x10)] public TraceHitboxData* HitboxData;
    [FieldOffset(0x50)] public uint Contents;
    [FieldOffset(0x78)] public Vector3 StartPos;
    [FieldOffset(0x84)] public Vector3 EndPos;
    [FieldOffset(0x90)] public Vector3 Normal;
    [FieldOffset(0x9C)] public Vector3 Position;
    [FieldOffset(0xAC)] public float Fraction;
    [FieldOffset(0xB6)] public bool AllSolid;
}

public struct Masks
{
    public const ulong MASK_ALL = ~0UL;
    public static readonly ulong MASK_SOLID = (ulong)Contents.SOLID | (ulong)Contents.WINDOW | (ulong)Contents.PLAYER | (ulong)Contents.NPC | (ulong)Contents.PASS_BULLETS;
    public static readonly ulong MASK_PLAYERSOLID = (ulong)Contents.SOLID | (ulong)Contents.PLAYER_CLIP | (ulong)Contents.WINDOW | (ulong)Contents.PLAYER | (ulong)Contents.NPC | (ulong)Contents.PASS_BULLETS;
    public static readonly ulong MASK_NPCSOLID = (ulong)Contents.SOLID | (ulong)Contents.NPC_CLIP | (ulong)Contents.WINDOW | (ulong)Contents.PLAYER | (ulong)Contents.NPC | (ulong)Contents.PASS_BULLETS;
    public static readonly ulong MASK_NPCFLUID = (ulong)Contents.SOLID | (ulong)Contents.NPC_CLIP | (ulong)Contents.WINDOW | (ulong)Contents.PLAYER | (ulong)Contents.NPC;
    public static readonly ulong MASK_WATER = (ulong)Contents.WATER | (ulong)Contents.SLIME;
    public static readonly ulong MASK_SHOT = (ulong)Contents.SOLID | (ulong)Contents.PLAYER | (ulong)Contents.NPC | (ulong)Contents.WINDOW | (ulong)Contents.DEBRIS | (ulong)Contents.HITBOX;
    public static readonly ulong MASK_SHOT_BRUSHONLY = (ulong)Contents.SOLID | (ulong)Contents.WINDOW | (ulong)Contents.DEBRIS;
    public static readonly ulong MASK_SHOT_HULL = (ulong)Contents.SOLID | (ulong)Contents.PLAYER | (ulong)Contents.NPC | (ulong)Contents.WINDOW | (ulong)Contents.DEBRIS | (ulong)Contents.PASS_BULLETS;
    public static readonly ulong MASK_SHOT_PORTAL = (ulong)Contents.SOLID | (ulong)Contents.WINDOW | (ulong)Contents.PLAYER | (ulong)Contents.NPC;
    public static readonly ulong MASK_SOLID_BRUSHONLY = (ulong)Contents.SOLID | (ulong)Contents.WINDOW | (ulong)Contents.PASS_BULLETS;
    public static readonly ulong MASK_PLAYERSOLID_BRUSHONLY = (ulong)Contents.SOLID | (ulong)Contents.WINDOW | (ulong)Contents.PLAYER_CLIP | (ulong)Contents.PASS_BULLETS;
    public static readonly ulong MASK_NPCSOLID_BRUSHONLY = (ulong)Contents.SOLID | (ulong)Contents.WINDOW | (ulong)Contents.NPC_CLIP | (ulong)Contents.PASS_BULLETS;

    public static ulong CreateCustomMask(params Contents[] contents)
    {
        ulong mask = 0;
        foreach (var content in contents)
        {
            mask |= (ulong)content;
        }
        return mask;
    }
}

[Flags]
public enum Contents : ulong
{
    EMPTY = 0x0,

    SOLID = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_SOLID,
    HITBOX = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_HITBOX,
    TRIGGER = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_TRIGGER,
    SKY = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_SKY,

    PLAYER_CLIP = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_PLAYER_CLIP,
    NPC_CLIP = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_NPC_CLIP,
    BLOCK_LOS = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_BLOCK_LOS,
    BLOCK_LIGHT = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_BLOCK_LIGHT,
    LADDER = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_LADDER,
    PICKUP = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_PICKUP,
    BLOCK_SOUND = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_BLOCK_SOUND,
    NODRAW = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_NODRAW,
    WINDOW = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_WINDOW,
    PASS_BULLETS = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_PASS_BULLETS,
    WORLD_GEOMETRY = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_WORLD_GEOMETRY,
    WATER = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_WATER,
    SLIME = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_SLIME,
    TOUCH_ALL = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_TOUCH_ALL,
    PLAYER = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_PLAYER,
    NPC = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_NPC,
    DEBRIS = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_DEBRIS,
    PHYSICS_PROP = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_PHYSICS_PROP,
    NAV_IGNORE = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_NAV_IGNORE,
    NAV_LOCAL_IGNORE = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_NAV_LOCAL_IGNORE,
    POST_PROCESSING_VOLUME = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_POST_PROCESSING_VOLUME,
    UNUSED_LAYER3 = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_UNUSED_LAYER3,
    CARRIED_OBJECT = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CARRIED_OBJECT,
    PUSHAWAY = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_PUSHAWAY,
    SERVER_ENTITY_ON_CLIENT = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_SERVER_ENTITY_ON_CLIENT,
    CARRIED_WEAPON = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CARRIED_WEAPON,
    STATIC_LEVEL = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_STATIC_LEVEL,

    CSGO_TEAM1 = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CSGO_TEAM1,
    CSGO_TEAM2 = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CSGO_TEAM2,
    CSGO_GRENADE_CLIP = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CSGO_GRENADE_CLIP,
    CSGO_DRONE_CLIP = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CSGO_DRONE_CLIP,
    CSGO_MOVEABLE = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CSGO_MOVEABLE,
    CSGO_OPAQUE = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CSGO_OPAQUE,
    CSGO_MONSTER = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CSGO_MONSTER,
    CSGO_UNUSED_LAYER = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CSGO_UNUSED_LAYER,
    CSGO_THROWN_GRENADE = 1UL << LayerIndices.LAYER_INDEX_CONTENTS_CSGO_THROWN_GRENADE
}

public static class LayerIndices
{
    public const int LAYER_INDEX_CONTENTS_SOLID = 0;
    public const int LAYER_INDEX_CONTENTS_HITBOX = 1;
    public const int LAYER_INDEX_CONTENTS_TRIGGER = 2;
    public const int LAYER_INDEX_CONTENTS_SKY = 3;
    public const int LAYER_INDEX_CONTENTS_PLAYER_CLIP = 4;
    public const int LAYER_INDEX_CONTENTS_NPC_CLIP = 5;
    public const int LAYER_INDEX_CONTENTS_BLOCK_LOS = 6;
    public const int LAYER_INDEX_CONTENTS_BLOCK_LIGHT = 7;
    public const int LAYER_INDEX_CONTENTS_LADDER = 8;
    public const int LAYER_INDEX_CONTENTS_PICKUP = 9;
    public const int LAYER_INDEX_CONTENTS_BLOCK_SOUND = 10;
    public const int LAYER_INDEX_CONTENTS_NODRAW = 11;
    public const int LAYER_INDEX_CONTENTS_WINDOW = 12;
    public const int LAYER_INDEX_CONTENTS_PASS_BULLETS = 13;
    public const int LAYER_INDEX_CONTENTS_WORLD_GEOMETRY = 14;
    public const int LAYER_INDEX_CONTENTS_WATER = 15;
    public const int LAYER_INDEX_CONTENTS_SLIME = 16;
    public const int LAYER_INDEX_CONTENTS_TOUCH_ALL = 17;
    public const int LAYER_INDEX_CONTENTS_PLAYER = 18;
    public const int LAYER_INDEX_CONTENTS_NPC = 19;
    public const int LAYER_INDEX_CONTENTS_DEBRIS = 20;
    public const int LAYER_INDEX_CONTENTS_PHYSICS_PROP = 21;
    public const int LAYER_INDEX_CONTENTS_NAV_IGNORE = 22;
    public const int LAYER_INDEX_CONTENTS_NAV_LOCAL_IGNORE = 23;
    public const int LAYER_INDEX_CONTENTS_POST_PROCESSING_VOLUME = 24;
    public const int LAYER_INDEX_CONTENTS_UNUSED_LAYER3 = 25;
    public const int LAYER_INDEX_CONTENTS_CARRIED_OBJECT = 26;
    public const int LAYER_INDEX_CONTENTS_PUSHAWAY = 27;
    public const int LAYER_INDEX_CONTENTS_SERVER_ENTITY_ON_CLIENT = 28;
    public const int LAYER_INDEX_CONTENTS_CARRIED_WEAPON = 29;
    public const int LAYER_INDEX_CONTENTS_STATIC_LEVEL = 30;
    
    public const int LAYER_INDEX_CONTENTS_CSGO_TEAM1 = 31;
    public const int LAYER_INDEX_CONTENTS_CSGO_TEAM2 = 32;
    public const int LAYER_INDEX_CONTENTS_CSGO_GRENADE_CLIP = 33;
    public const int LAYER_INDEX_CONTENTS_CSGO_DRONE_CLIP = 34;
    public const int LAYER_INDEX_CONTENTS_CSGO_MOVEABLE = 35;
    public const int LAYER_INDEX_CONTENTS_CSGO_OPAQUE = 36;
    public const int LAYER_INDEX_CONTENTS_CSGO_MONSTER = 37;
    public const int LAYER_INDEX_CONTENTS_CSGO_UNUSED_LAYER = 38;
    public const int LAYER_INDEX_CONTENTS_CSGO_THROWN_GRENADE = 39;
}