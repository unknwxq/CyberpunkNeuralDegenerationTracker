using System.Runtime.InteropServices;
using SynapseSec.Models;

namespace SynapseSec.Native;

// p/invoke wrapper for native c library cybersignal.dll
public static class CyberSignalNative
{
    [DllImport("cybersignal", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int calc_humanity_index(int stress_level, int ghz_load, int blocker_doses);

    // validates inputs and calls native c function, returning Result
    public static Result<int, NeuralError> CheckHumanityStability(
        NeuralSession session,
        int blockerDoses,
        Func<int, int, int, int>? nativeCaller = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (blockerDoses < 0 || session.StressLevel < 0 || session.StressLevel > 10 || session.NeuralLoadGhz < 0)
        {
            return new Result<int, NeuralError>.Failure(
                new NeuralError(ErrorCode.NativeCallFailed, $"Invalid metrics for native humanity check on {session.OperatorId}."));
        }

        try
        {
            var invoke = nativeCaller ?? calc_humanity_index;
            int humanity = invoke(
                (int)Math.Round(session.StressLevel),
                (int)Math.Round(session.NeuralLoadGhz),
                blockerDoses);

            if (humanity < 0)
            {
                return new Result<int, NeuralError>.Failure(
                    new NeuralError(ErrorCode.NativeCallFailed, "Native C function rejected out-of-range values (-1)."));
            }

            return new Result<int, NeuralError>.Success(humanity);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return new Result<int, NeuralError>.Failure(
                new NeuralError(ErrorCode.NativeCallFailed, $"Native library cybersignal could not be loaded: {ex.Message}"));
        }
    }
}