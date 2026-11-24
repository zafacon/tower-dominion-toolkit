using System.Runtime.InteropServices;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

/// <summary>Version-checked, temporary native acceptance branch override.</summary>
internal sealed class BrytaNativeAcceptance : IDisposable
{
    private static readonly byte[] Original = { 0x0f, 0x85, 0xbd, 0, 0, 0 };
    private static readonly byte[] Accept = { 0xe9, 0xbe, 0, 0, 0, 0x90 };
    private static readonly byte[] Context = { 0x4c, 0x8b, 0x05, 0x01, 0x99, 0x43, 0x03, 0x8b, 0x56, 0x50, 0x49, 0x8b, 0xcd, 0xe8, 0xee, 0x28, 0xea, 0x00, 0x84, 0xc0 };
    private readonly IntPtr address;
    private BrytaNativeAcceptance(IntPtr address) => this.address = address;
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string name);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint protection, out uint old);
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll")] private static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);
    private static bool Matches(IntPtr address, byte[] expected)
    {
        var actual = new byte[expected.Length];
        Marshal.Copy(address, actual, 0, actual.Length);
        return actual.SequenceEqual(expected);
    }
    private static bool Write(IntPtr address, byte[] bytes)
    {
        var size = (UIntPtr)bytes.Length;
        if (!VirtualProtect(address, size, 0x40, out uint old)) return false;
        try { Marshal.Copy(bytes, 0, address, bytes.Length); FlushInstructionCache(GetCurrentProcess(), address, size); }
        finally { VirtualProtect(address, size, old, out _); }
        return Matches(address, bytes);
    }
    internal static BrytaNativeAcceptance? TryEnter()
    {
        try
        {
            var module = GetModuleHandle("GameAssembly.dll");
            if (module == IntPtr.Zero) return null;
            var address = IntPtr.Add(module, 0x501054);
            if (!Matches(IntPtr.Add(address, -Context.Length), Context) || !Matches(address, Original))
            { MelonLogger.Warning("[Toolkit Bryta] Native acceptance signature mismatch; reroll refused."); return null; }
            if (!Write(address, Accept)) return null;
            BrytaRerollDiagnostics.Report("native-acceptance", "branch bypass active");
            return new BrytaNativeAcceptance(address);
        }
        catch (Exception ex) { MelonLogger.Warning($"[Toolkit Bryta] Native acceptance unavailable: {ex.Message}"); return null; }
    }
    public void Dispose()
    {
        if (!Write(address, Original)) MelonLogger.Error("[Toolkit Bryta] Failed to restore native acceptance branch; restart game.");
        else BrytaRerollDiagnostics.Report("native-acceptance", "original branch restored");
    }
}
