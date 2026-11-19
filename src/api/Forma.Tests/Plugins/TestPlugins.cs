using System.Text;
using System.Text.Json.Nodes;
using Wasmtime;

namespace Forma.Tests.Plugins;

/// <summary>Small plugins written in WebAssembly text, for testing the sandbox and the events.</summary>
public static class TestPlugins
{
    /// <summary>
    /// A module that follows the plugin ABI: a bump allocator from 64 KB up, <c>forma.log</c> imported as <c>$log</c>,
    /// and <paramref name="handle"/> as the body of <c>forma_handle(param $p i32) (param $len i32) (result i64)</c>.
    /// <paramref name="data"/> is placed at address 0.
    /// </summary>
    public static string Wat(string handle, string data = "", string memory = "1") => $$"""
        (module
          (import "forma" "log" (func $log (param i32 i32 i32)))
          (memory (export "memory") {{memory}})
          (global $next (mut i32) (i32.const 65536))
          (data (i32.const 0) "{{Escape(data)}}")
          (func (export "forma_alloc") (param $len i32) (result i32)
            (local $p i32)
            (local.set $p (global.get $next))
            (global.set $next (i32.add (global.get $next) (local.get $len)))
            (if (i32.gt_u (global.get $next) (i32.mul (memory.size) (i32.const 65536)))
              (then (drop (memory.grow (i32.const 16)))))
            (local.get $p))
          (func (export "forma_handle") (param $p i32) (param $len i32) (result i64)
            {{handle}}))
        """;

    /// <summary>Returns <c>pointer &lt;&lt; 32 | length</c>, the way <c>forma_handle</c> returns output.</summary>
    public static string Return(string pointer, string length) =>
        $"(i64.or (i64.shl (i64.extend_i32_u {pointer}) (i64.const 32)) (i64.extend_i32_u {length}))";

    /// <summary>Returns its input unchanged (and logs it).</summary>
    public static string Echo => Wat($"""
        (call $log (i32.const 1) (local.get $p) (local.get $len))
        {Return("(local.get $p)", "(local.get $len)")}
        """);

    /// <summary>Returns <paramref name="output"/> whatever the input.</summary>
    public static string Returns(string output) =>
        Wat(Return("(i32.const 0)", $"(i32.const {Encoding.UTF8.GetByteCount(output)})"), output);

    /// <summary>A <c>page.check</c> plugin that returns <paramref name="issues"/>.</summary>
    public static string Issues(params JsonObject[] issues) =>
        Returns(new JsonObject { ["issues"] = new JsonArray(issues.Select(i => (JsonNode)i).ToArray()) }.ToJsonString());

    /// <summary>Traps on every call.</summary>
    public static string Crashes => Wat("unreachable");

    public static byte[] Compile(string wat) => Module.ConvertText(wat);

    /// <summary>Writes a plugin folder (<c>plugin.json</c> and <c>plugin.wasm</c>) under <paramref name="root"/>.</summary>
    public static void Install(string root, string id, string wat, params string[] events)
    {
        var folder = Directory.CreateDirectory(Path.Combine(root, id)).FullName;
        File.WriteAllText(Path.Combine(folder, "plugin.json"), new JsonObject
        {
            ["id"] = id,
            ["name"] = $"Test {id}",
            ["version"] = "1.0.0",
            ["events"] = new JsonArray(events.Select(e => (JsonNode)e).ToArray()),
        }.ToJsonString());
        File.WriteAllBytes(Path.Combine(folder, "plugin.wasm"), Compile(wat));
    }

    private static string Escape(string data)
    {
        var builder = new StringBuilder();
        foreach (var b in Encoding.UTF8.GetBytes(data))
        {
            builder.Append(b is >= 0x20 and < 0x7f and not (byte)'"' and not (byte)'\\' ? ((char)b).ToString() : $"\\{b:x2}");
        }

        return builder.ToString();
    }
}
