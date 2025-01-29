using System.Runtime.CompilerServices;

namespace StrafeHUD.Extensions;
public class CBaseUserCmd
{
    public CBaseUserCmd(IntPtr pointer)
    {
        Handle = pointer;
    }

    public IntPtr Handle { get; set; }
    public float ForwardMove => GetForwardMove();
    public float SideMove => GetSideMove();
    public unsafe float GetForwardMove()
    {
        var ForwardMove = Unsafe.Read<float>((void*)(Handle + 0x50));
        return ForwardMove;
    }
    public unsafe float GetSideMove()
    {
        var SideMove = Unsafe.Read<float>((void*)(Handle + 0x54));
        return SideMove;
    }
}