using System;
using System.Runtime.InteropServices;

namespace Imger
{
    internal static class GpuEnforcer
    {
        private const string NvDll64 = "nvapi64.dll";
        private const string NvDll32 = "nvapi.dll";
        private const string AmdDll64 = "atiadlxx.dll";
        private const string AmdDll32 = "atiadlxy.dll";

        private static readonly bool Is64 = Environment.Is64BitProcess;

        #region ── NVIDIA ──────────────────────────────
        [DllImport(NvDll64, EntryPoint = "NvAPI_Initialize", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvAPI_Initialize64();

        [DllImport(NvDll32, EntryPoint = "NvAPI_Initialize", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvAPI_Initialize32();

        private static int NvAPI_Initialize() =>
            Is64 ? NvAPI_Initialize64() : NvAPI_Initialize32();
        #endregion

        #region ── AMD ────────────────────────────────
        [DllImport(AmdDll64, EntryPoint = "ADL2_Main_Control_Create", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_Main_Control_Create64(IntPtr c, int e, out IntPtr ctx);

        [DllImport(AmdDll32, EntryPoint = "ADL2_Main_Control_Create", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_Main_Control_Create32(IntPtr c, int e, out IntPtr ctx);

        private static int ADL2_Main_Control_Create(IntPtr c, int e, out IntPtr ctx) =>
            Is64 ? ADL2_Main_Control_Create64(c, e, out ctx)
                 : ADL2_Main_Control_Create32(c, e, out ctx);
        #endregion

        public static void ForceDedicatedGpu()
        {
            try
            {
                if (NvAPI_Initialize() == 0)
                {
                    Console.WriteLine("[GpuEnforcer] NVIDIA GPU detected & forced.");
                    return;
                }
            }
            catch { }

            try
            {
                IntPtr ctx;
                if (ADL2_Main_Control_Create(IntPtr.Zero, 1, out ctx) == 0)
                {
                    Console.WriteLine("[GpuEnforcer] AMD GPU detected & forced.");
                    return;
                }
            }
            catch { }
        }
    }
}
