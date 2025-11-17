using System.Runtime.InteropServices;
using WorldGen.Debugging.RenderDoc.Bindings;

namespace WorldGen.Debugging.RenderDoc;

/// <summary>
/// RenderDoc is a frame-capture based graphics debugger.
/// </summary>
public unsafe class RenderDoc
{
    /// <summary>
    /// The RenderDoc <see cref="http://renderdoc.org/docs/in_application_api.html">API</see>.
    /// </summary>
    public readonly RenderDocApi API;

    public bool IsCapturing => API.IsFrameCapturing() == 1;

    /// <summary>
    /// Attempts to load RenderDoc.
    /// </summary>
    /// <param name="renderDoc">The RenderDoc instance.</param>
    /// <returns>Whether RenderDoc was successfully loaded.</returns>
    public static RenderDoc? Load()
    {
#if !DEBUG
    	return null;
#endif
        var libName = GetRenderDocLibName();
        if (NativeLibrary.TryLoad(libName, out var lib) ||
            NativeLibrary.TryLoad(libName, typeof(RenderDoc).Assembly, null, out lib))
        {
            var renderDoc = new RenderDoc(lib);
            Console.WriteLine("RenderDoc loaded");
            renderDoc.API.SetCaptureFilePathTemplate("captures/WorldGenCapture");
            return renderDoc;
        }

        return null;
    }

    private unsafe RenderDoc(IntPtr nativeLib)
    {
        NativeLibrary.TryGetExport(nativeLib, "RENDERDOC_GetAPI", out IntPtr funcPtr);
        var getApiDelegate = Marshal.GetDelegateForFunctionPointer<pRENDERDOC_GetAPI>(funcPtr);
        void* apiPointers;
        int result = getApiDelegate(RENDERDOC_Version.eRENDERDOC_API_Version_1_4_1, &apiPointers);
        if (result != 1)
        {
            throw new InvalidOperationException("Failed to load RenderDoc API.");
        }

        API = Marshal.PtrToStructure<RenderDocApi>((IntPtr)apiPointers);
    }

    public void ToggleCapture()
    {
        if (IsCapturing)
            API.EndFrameCapture(IntPtr.Zero, IntPtr.Zero);
        else
            API.StartFrameCapture(IntPtr.Zero, IntPtr.Zero);
    }

    private static string GetRenderDocLibName()
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            // On Unix-like systems, RenderDoc is typically installed as a shared library.
            return "librenderdoc.so";
        }
        return "renderdoc.dll";
    }
}
