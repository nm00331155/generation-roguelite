using System;
using System.Runtime.InteropServices;

namespace GenerationRoguelite.SLM;

public sealed class SLMBridge : IDisposable
{
    private bool _nativeUnavailable;
    private bool _disposed;

    [DllImport("generation_slm", EntryPoint = "generate_event_json_utf8", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GenerateEventJsonUtf8([MarshalAs(UnmanagedType.LPUTF8Str)] string prompt);

    [DllImport("generation_slm", EntryPoint = "free_generated_buffer", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FreeGeneratedBuffer(IntPtr buffer);

    public bool TryGenerateEventJson(string prompt, out string json, out string error)
    {
        json = string.Empty;
        error = string.Empty;

        if (_disposed)
        {
            error = "SLM bridge is disposed.";
            return false;
        }

        if (_nativeUnavailable)
        {
            error = "SLM native library unavailable.";
            return false;
        }

        var generatedPtr = IntPtr.Zero;

        try
        {
            generatedPtr = GenerateEventJsonUtf8(prompt);
            if (generatedPtr == IntPtr.Zero)
            {
                error = "SLM returned null buffer.";
                return false;
            }

            json = Marshal.PtrToStringUTF8(generatedPtr) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "SLM returned empty response.";
                return false;
            }

            return true;
        }
        catch (DllNotFoundException ex)
        {
            _nativeUnavailable = true;
            error = $"SLM DLL not found: {ex.Message}";
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            _nativeUnavailable = true;
            error = $"SLM entry point missing: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            error = $"SLM runtime error: {ex.Message}";
            return false;
        }
        finally
        {
            if (generatedPtr != IntPtr.Zero)
            {
                try
                {
                    FreeGeneratedBuffer(generatedPtr);
                }
                catch
                {
                }
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
