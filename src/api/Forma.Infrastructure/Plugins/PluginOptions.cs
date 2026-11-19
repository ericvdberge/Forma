namespace Forma.Infrastructure.Plugins;

/// <summary>
/// Where plugins are installed and the sandbox limits every invocation runs under (configuration section <c>Plugins</c>).
/// </summary>
public sealed class PluginOptions
{
    public const string SectionName = "Plugins";

    /// <summary>Folder with one sub-folder per plugin (relative paths resolve from the working directory).</summary>
    public string Path { get; set; } = "plugins";

    /// <summary>
    /// WebAssembly instructions one invocation may execute (Wasmtime fuel). Deterministic: the same input always runs
    /// out at the same point, unlike a wall-clock timeout.
    /// </summary>
    public long FuelPerInvocation { get; set; } = 200_000_000;

    /// <summary>Wall-clock limit for one invocation, a backstop for the fuel limit.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Largest linear memory a plugin may grow to.</summary>
    public long MaxMemoryBytes { get; set; } = 32 * 1024 * 1024;

    /// <summary>Largest output a plugin may return.</summary>
    public int MaxOutputBytes { get; set; } = 256 * 1024;

    /// <summary>Log lines kept per invocation; more are counted but dropped.</summary>
    public int MaxLogLines { get; set; } = 50;
}
