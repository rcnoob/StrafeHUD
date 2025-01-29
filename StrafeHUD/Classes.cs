using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace StrafeHUD;

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

public class PlayerStats
{ 
    public CCSPlayer_MovementServices? MovementService { get; set; }
    public int TickCount { get; set; } = 0;
    public int FramesOnGround { get; set; } = 0;
    public int FramesInAir { get; set; } = 0;
    public float ForwardMove { get; set; } = 0;
    public float LastForwardMove { get; set; } = 0;
    public float SideMove { get; set; } = 0;
    public float LastSideMove { get; set; } = 0;
    public float Stamina { get; set; } = 0;
    public float LastStamina { get; set; } = 0;
    public PlayerFlags Flags { get; set; } = 0;
    public PlayerFlags LastFlags { get; set; } = 0;
    public PlayerButtons Buttons { get; set; } = 0;
    public PlayerButtons LastButtons { get; set; } = 0;
    public QAngle Angles { get; set; } = new();
    public QAngle LastAngles { get; set; } = new();
    public Vector Position { get; set; } = new();
    public Vector LastPosition { get; set; } = new();
    public Vector LastGroundPosition { get; set; } = new();
    public Vector Velocity { get; set; } = new();
    public Vector LastVelocity { get; set; } = new();
    public bool LandedDucked = false;
    
    public float JumpGroundZ { get; set; } = 0;
    public Vector JumpPosition { get; set; } = new();
    public QAngle JumpAngles { get; set; } = new();
    public float LandGroundZ { get; set; } = 0;
    public Vector LandPosition { get; set; } = new();
    
    public int ForwardReleaseFrame { get; set; } = 0;
    public int JumpFrame { get; set; } = 0;
    public bool TrackingJump = false;
    public bool FailedJump = false;
    public int PrespeedFog { get; set; } = 0;
    public float PrespeedStamina { get; set; } = 0;
    
    // jumps!
    public float JumpDistance { get; set; } = 0;
    public float JumpPrespeed { get; set; } = 0;
    public float JumpMaxspeed { get; set; } = 0;
    public float JumpVeer { get; set; } = 0;
    public float JumpAirpath { get; set; } = 0;
    public float JumpSync { get; set; } = 0;
    public float JumpEdge { get; set; } = 0;
    public float JumpLandEdge { get; set; } = 0;
    public float JumpBlockDistance { get; set; } = 0;
    public float JumpHeight { get; set; } = 0;
    public float JumpoffAngle { get; set; } = 0;
    public int JumpAirtime { get; set; } = 0;
    public int JumpForwardRelease { get; set; } = 0;
    public int JumpOverlap { get; set; } = 0;
    public int JumpDeadair { get; set; } = 0;
    
    // strafes!
    public int StrafeCount { get; set; } = 0;
    public float[] StrafeSync { get; set; } = new float[32];
    public float[] StrafeGain { get; set; } = new float[32];
    public float[] StrafeLoss { get; set; } = new float[32];
    public float[] StrafeMax { get; set; } = new float[32];
    public int[] StrafeAirtime { get; set; } = new int[32];
    public int[] StrafeOverlap { get; set; } = new int[32];
    public int[] StrafeDeadair { get; set; } = new int[32];
    public float[] StrafeAvgGain { get; set; } = new float[32];
    public float[] StrafeAvgEfficiency { get; set; } = new float[32];
    public int[] StrafeAvgEfficiencyCount { get; set; } = new int[32];
    public float[] StrafeMaxEfficiency { get; set; } = new float[32];
    
    public StrafeType[] StrafeGraph { get; set; } = new StrafeType[32];
    public float[] MouseGraph { get; set; } = new float[32];
}