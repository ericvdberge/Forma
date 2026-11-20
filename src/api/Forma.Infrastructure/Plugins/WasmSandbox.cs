using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using Wasmtime;

namespace Forma.Infrastructure.Plugins;

/// <summary>Compiles plugin modules and runs them, each invocation in its own sandbox.</summary>
/// <remarks>
/// The sandbox is what makes third-party code safe to run inside the API (<c>docs/design/plugins.md</c> § 4):
/// <list type="bullet">
/// <item>No WASI: a plugin has no files, network, clock, randomness or environment. Its only import is <c>forma.log</c>.</item>
/// <item>A fresh store and instance per invocation, so nothing carries over between calls or between plugins.</item>
/// <item>Fuel (instructions) and an epoch deadline (wall clock) stop endless loops; a memory limit stops runaway growth.</item>
/// <item>Input and output pass through the plugin's own linear memory and are bounds-checked on the way out.</item>
/// </list>
/// </remarks>
public interface IWasmSandbox
{
    /// <summary>Compiles <paramref name="wasm"/> and checks it follows the plugin ABI.</summary>
    /// <exception cref="PluginLoadException">The bytes are not a valid module, or it imports or exports the wrong things.</exception>
    Module Compile(string name, ReadOnlySpan<byte> wasm);

    /// <summary>Runs the module's <c>forma_handle</c> with <paramref name="input"/> (JSON) in a new sandbox.</summary>
    /// <remarks>Never throws for anything the plugin does: traps, limits and bad output come back as a failed invocation.</remarks>
    PluginInvocation Invoke(Module module, string input);
}

public sealed class WasmSandbox : IWasmSandbox, IDisposable
{
    /// <summary>How often the engine's epoch advances; the timeout is counted in these ticks.</summary>
    private static readonly TimeSpan EpochTick = TimeSpan.FromMilliseconds(10);

    private const string ImportModule = "forma";
    private const string MemoryExport = "memory";
    private const string AllocExport = "forma_alloc";
    private const string HandleExport = "forma_handle";
    private const int MaxLogLineLength = 1000;

    private readonly PluginOptions _options;
    private readonly Engine _engine;
    private readonly Timer _epochTimer;

    public WasmSandbox(IOptions<PluginOptions> options)
    {
        _options = options.Value;
        using var config = new Config()
            .WithFuelConsumption(true)
            .WithEpochInterruption(true)
            // Plugins are small; keep their stacks small too (deep recursion traps instead of using up the thread).
            .WithMaximumStackSize(512 * 1024);
        _engine = new Engine(config);
        _epochTimer = new Timer(_ => _engine.IncrementEpoch(), null, EpochTick, EpochTick);
    }

    public Module Compile(string name, ReadOnlySpan<byte> wasm)
    {
        Module module;
        try
        {
            module = Module.FromBytes(_engine, name, wasm);
        }
        catch (WasmtimeException e)
        {
            throw new PluginLoadException($"Not a valid WebAssembly module: {FirstLine(e.Message)}");
        }

        try
        {
            CheckAbi(module);
        }
        catch
        {
            module.Dispose();
            throw;
        }

        return module;
    }

    /// <summary>The module imports only what the host provides and exports what the host calls.</summary>
    private static void CheckAbi(Module module)
    {
        foreach (var import in module.Imports)
        {
            var known = import is FunctionImport function
                && import.ModuleName == ImportModule
                && import.Name == "log"
                && function.Parameters.SequenceEqual([ValueKind.Int32, ValueKind.Int32, ValueKind.Int32])
                && function.Results.Count == 0;
            if (!known)
            {
                // Most often a WASI import: the plugin was built for wasm32-wasip1 instead of wasm32-unknown-unknown.
                throw new PluginLoadException(
                    $"The module imports '{import.ModuleName}.{import.Name}', which plugins can't use. The only import is 'forma.log'.");
            }
        }

        var exports = module.Exports.ToDictionary(e => e.Name);
        if (!exports.TryGetValue(MemoryExport, out var memory) || memory is not Wasmtime.MemoryExport)
        {
            throw new PluginLoadException($"The module must export its memory as '{MemoryExport}'.");
        }

        RequireFunction(exports, AllocExport, [ValueKind.Int32], [ValueKind.Int32]);
        RequireFunction(exports, HandleExport, [ValueKind.Int32, ValueKind.Int32], [ValueKind.Int64]);
    }

    private static void RequireFunction(Dictionary<string, Export> exports, string name, ValueKind[] parameters, ValueKind[] results)
    {
        if (!exports.TryGetValue(name, out var export)
            || export is not FunctionExport function
            || !function.Parameters.SequenceEqual(parameters)
            || !function.Results.SequenceEqual(results))
        {
            throw new PluginLoadException(
                $"The module must export '{name}({string.Join(", ", parameters)}) -> {string.Join(", ", results)}'.");
        }
    }

    public PluginInvocation Invoke(Module module, string input)
    {
        var state = new InvocationState(_options.MaxLogLines);
        var stopwatch = Stopwatch.StartNew();

        using var store = new Store(_engine, state);
        store.SetLimits(memorySize: _options.MaxMemoryBytes, tableElements: 10_000, instances: 1, tables: 1, memories: 1);
        store.Fuel = (ulong)_options.FuelPerInvocation;
        store.SetEpochDeadline((ulong)Math.Max(1, _options.Timeout / EpochTick));

        PluginInvocation Result(bool succeeded, string? output, string? error) => new(
            succeeded,
            output,
            error,
            state.Logs,
            state.DroppedLogLines,
            _options.FuelPerInvocation - (long)store.Fuel,
            stopwatch.Elapsed);

        try
        {
            using var linker = new Linker(_engine);
            linker.DefineFunction(ImportModule, "log", (Caller caller, int level, int pointer, int length) => Log(caller, level, pointer, length));

            var instance = linker.Instantiate(store, module);
            var memory = instance.GetMemory(MemoryExport)!;
            var alloc = instance.GetFunction<int, int>(AllocExport)!;
            var handle = instance.GetFunction<int, int, long>(HandleExport)!;

            var bytes = Encoding.UTF8.GetBytes(input);
            var inputPointer = alloc(bytes.Length);
            if (!InBounds(memory, inputPointer, bytes.Length))
            {
                return Result(false, null, "forma_alloc returned memory outside the module's memory.");
            }

            bytes.CopyTo(memory.GetSpan(inputPointer, bytes.Length));

            var packed = handle(inputPointer, bytes.Length);
            // (pointer << 32) | length; 0 means "no output".
            var outputPointer = (int)(packed >>> 32);
            var outputLength = (int)(packed & 0xFFFF_FFFF);
            if (outputLength == 0)
            {
                return Result(true, null, null);
            }

            if (outputLength < 0 || outputLength > _options.MaxOutputBytes)
            {
                return Result(false, null, $"The output is larger than {_options.MaxOutputBytes / 1024} KB.");
            }

            if (!InBounds(memory, outputPointer, outputLength))
            {
                return Result(false, null, "forma_handle returned output outside the module's memory.");
            }

            var output = Encoding.UTF8.GetString(memory.GetSpan(outputPointer, outputLength));
            return Result(true, output, null);
        }
        catch (TrapException e)
        {
            return Result(false, null, e.Type switch
            {
                TrapCode.OutOfFuel => "The plugin used up its instruction budget (fuel) and was stopped.",
                TrapCode.Interrupt => $"The plugin took longer than {_options.Timeout.TotalMilliseconds:0} ms and was stopped.",
                TrapCode.StackOverflow => "The plugin overflowed its stack and was stopped.",
                _ => $"The plugin crashed: {FirstLine(e.Message)}",
            });
        }
        catch (WasmtimeException e)
        {
            return Result(false, null, $"The plugin couldn't run: {FirstLine(e.Message)}");
        }
    }

    private static bool InBounds(Memory memory, int pointer, int length) =>
        pointer >= 0 && length >= 0 && (long)pointer + length <= memory.GetLength();

    /// <summary><c>forma.log(level, pointer, length)</c>: level 0 debug, 1 info, 2 warning, 3 error.</summary>
    private static void Log(Caller caller, int level, int pointer, int length)
    {
        var state = (InvocationState)caller.GetData()!;
        if (state.Logs.Count >= state.MaxLines)
        {
            state.DroppedLogLines++;
            return;
        }

        var memory = caller.GetMemory(MemoryExport);
        if (memory is null || !InBounds(memory, pointer, length))
        {
            // Not worth a trap: the line is lost and the plugin carries on.
            state.Logs.Add(new PluginLogLine(PluginLogLevel.Warning, "(log line outside the module's memory)"));
            return;
        }

        var text = Encoding.UTF8.GetString(memory.GetSpan(pointer, Math.Min(length, MaxLogLineLength * 4)));
        if (text.Length > MaxLogLineLength)
        {
            text = text[..MaxLogLineLength] + "…";
        }

        state.Logs.Add(new PluginLogLine(Enum.IsDefined((PluginLogLevel)level) ? (PluginLogLevel)level : PluginLogLevel.Info, text));
    }

    private static string FirstLine(string message) => message.Split('\n', 2)[0].Trim();

    public void Dispose()
    {
        _epochTimer.Dispose();
        _engine.Dispose();
    }

    private sealed class InvocationState(int maxLines)
    {
        public int MaxLines { get; } = maxLines;
        public List<PluginLogLine> Logs { get; } = [];
        public int DroppedLogLines { get; set; }
    }
}

/// <summary>A plugin could not be loaded: its manifest or module is wrong.</summary>
public sealed class PluginLoadException(string message) : Exception(message);

public enum PluginLogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
}

public sealed record PluginLogLine(PluginLogLevel Level, string Message);

/// <summary>What happened when a plugin ran.</summary>
/// <param name="Succeeded">False when it trapped, hit a limit or returned bad output.</param>
/// <param name="Output">The JSON it returned; null when it returned nothing or failed.</param>
/// <param name="Error">Why it failed, in plain language.</param>
/// <param name="Logs">What it logged with <c>forma.log</c>, up to the configured number of lines.</param>
/// <param name="DroppedLogLines">Lines logged past that limit.</param>
/// <param name="FuelConsumed">Instructions used, roughly.</param>
/// <param name="Duration">Wall-clock time, instantiation included.</param>
public sealed record PluginInvocation(
    bool Succeeded,
    string? Output,
    string? Error,
    IReadOnlyList<PluginLogLine> Logs,
    int DroppedLogLines,
    long FuelConsumed,
    TimeSpan Duration);
