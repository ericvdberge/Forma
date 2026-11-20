using Forma.Infrastructure.Plugins;
using Microsoft.Extensions.Options;
using static Forma.Tests.Plugins.TestPlugins;

namespace Forma.Tests.Plugins;

public sealed class WasmSandboxTests : IDisposable
{
    private readonly WasmSandbox _sandbox = new(Options.Create(new PluginOptions
    {
        FuelPerInvocation = 5_000_000,
        Timeout = TimeSpan.FromSeconds(5),
        MaxMemoryBytes = 4 * 1024 * 1024,
        MaxOutputBytes = 1024,
        MaxLogLines = 3,
    }));

    private PluginInvocation Run(string wat, string input = """{"event":"test"}""") =>
        _sandbox.Invoke(_sandbox.Compile("test", Compile(wat)), input);

    [Fact]
    public void Input_reaches_the_plugin_and_its_output_and_logs_come_back()
    {
        var result = Run(Echo, """{"event":"page.published","data":{"title":"Héllo"}}""");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("""{"event":"page.published","data":{"title":"Héllo"}}""", result.Output);
        var line = Assert.Single(result.Logs);
        Assert.Equal(PluginLogLevel.Info, line.Level);
        Assert.Contains("Héllo", line.Message);
        Assert.True(result.FuelConsumed > 0);
    }

    [Fact]
    public void Returning_zero_means_no_output()
    {
        var result = Run(Wat("(i64.const 0)"));

        Assert.True(result.Succeeded);
        Assert.Null(result.Output);
    }

    [Fact]
    public void An_endless_loop_runs_out_of_fuel()
    {
        var result = Run(Wat("(loop $forever (br $forever)) (i64.const 0)"));

        Assert.False(result.Succeeded);
        Assert.Contains("fuel", result.Error);
    }

    [Fact]
    public void A_plugin_is_stopped_at_the_timeout_even_with_fuel_left()
    {
        using var sandbox = new WasmSandbox(Options.Create(new PluginOptions
        {
            FuelPerInvocation = long.MaxValue / 2,
            Timeout = TimeSpan.FromMilliseconds(50),
        }));
        var module = sandbox.Compile("slow", Compile(Wat("(loop $forever (br $forever)) (i64.const 0)")));

        var result = sandbox.Invoke(module, "{}");

        Assert.False(result.Succeeded);
        Assert.Contains("took longer than 50 ms", result.Error);
        Assert.InRange(result.Duration, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Memory_cant_grow_past_the_limit()
    {
        // 4 MB is 64 pages; asking for 100 more fails (memory.grow returns -1), and this plugin traps when it does.
        var result = Run(Wat("""
            (if (i32.eq (memory.grow (i32.const 100)) (i32.const -1)) (then unreachable))
            (i64.const 0)
            """));

        Assert.False(result.Succeeded);
        Assert.Contains("crashed", result.Error);
    }

    [Fact]
    public void A_module_that_starts_with_too_much_memory_doesnt_run()
    {
        var result = Run(Wat("(i64.const 0)", memory: "100"));

        Assert.False(result.Succeeded);
        Assert.Contains("couldn't run", result.Error);
    }

    [Fact]
    public void Output_outside_the_plugins_memory_is_refused()
    {
        var result = Run(Wat(Return("(i32.const 0x7fff0000)", "(i32.const 100)")));

        Assert.False(result.Succeeded);
        Assert.Contains("outside the module's memory", result.Error);
    }

    [Fact]
    public void Output_over_the_limit_is_refused()
    {
        var result = Run(Wat(Return("(i32.const 0)", "(i32.const 2000)")));

        Assert.False(result.Succeeded);
        Assert.Contains("larger than", result.Error);
    }

    [Fact]
    public void Log_lines_past_the_limit_are_counted_but_dropped()
    {
        var result = Run(Wat("""
            (local $i i32)
            (loop $again
              (call $log (i32.const 2) (i32.const 0) (i32.const 2))
              (local.set $i (i32.add (local.get $i) (i32.const 1)))
              (br_if $again (i32.lt_u (local.get $i) (i32.const 10))))
            (i64.const 0)
            """, data: "hi"));

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Logs.Count);
        Assert.Equal(7, result.DroppedLogLines);
        Assert.All(result.Logs, l => Assert.Equal(new PluginLogLine(PluginLogLevel.Warning, "hi"), l));
    }

    [Fact]
    public void Nothing_carries_over_between_invocations()
    {
        // Adds 1 to the byte at address 0 ("0") and returns it: always "1", because every call gets a fresh instance.
        var module = _sandbox.Compile("counter", Compile(Wat($"""
            (i32.store8 (i32.const 0) (i32.add (i32.load8_u (i32.const 0)) (i32.const 1)))
            {Return("(i32.const 0)", "(i32.const 1)")}
            """, data: "0")));

        Assert.Equal("1", _sandbox.Invoke(module, "{}").Output);
        Assert.Equal("1", _sandbox.Invoke(module, "{}").Output);
    }

    [Fact]
    public void Wasi_imports_are_refused_when_loading()
    {
        var wat = """
            (module
              (import "wasi_snapshot_preview1" "fd_write" (func (param i32 i32 i32 i32) (result i32)))
              (memory (export "memory") 1)
              (func (export "forma_alloc") (param i32) (result i32) (i32.const 0))
              (func (export "forma_handle") (param i32 i32) (result i64) (i64.const 0)))
            """;

        var error = Assert.Throws<PluginLoadException>(() => _sandbox.Compile("wasi", Compile(wat)));
        Assert.Contains("wasi_snapshot_preview1.fd_write", error.Message);
    }

    [Fact]
    public void Modules_without_the_abi_exports_are_refused()
    {
        var wat = """(module (memory (export "memory") 1) (func (export "forma_alloc") (param i32) (result i32) (i32.const 0)))""";

        var error = Assert.Throws<PluginLoadException>(() => _sandbox.Compile("partial", Compile(wat)));
        Assert.Contains("forma_handle", error.Message);
    }

    [Fact]
    public void Bytes_that_arent_webassembly_are_refused()
    {
        var error = Assert.Throws<PluginLoadException>(() => _sandbox.Compile("junk", "not wasm"u8));
        Assert.Contains("Not a valid WebAssembly module", error.Message);
    }

    public void Dispose() => _sandbox.Dispose();
}
