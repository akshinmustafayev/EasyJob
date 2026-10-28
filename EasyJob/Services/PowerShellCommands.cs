using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace EasyJob.Services;

public record PsCommand(string Name, string Type, string Module);

public record PsCommandHelp(string Syntax, string Synopsis, string Description, IReadOnlyList<string> Examples);

/// <summary>Asks the installed PowerShell which commands exist and what they do.</summary>
public static class PowerShellCommands
{
    static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static async Task<List<PsCommand>> ListAsync(string pwsh, CancellationToken ct = default)
    {
        const string script =
            "$ErrorActionPreference='SilentlyContinue';" +
            "$items=@(Get-Command -CommandType Cmdlet,Function,Alias | ForEach-Object { [pscustomobject]@{ n=$_.Name; t=[string]$_.CommandType; m=[string]$_.ModuleName } });" +
            "ConvertTo-Json -InputObject $items -Compress";

        var json = await RunAsync(pwsh, script, ct);
        using var doc = JsonDocument.Parse(json);
        var result = new List<PsCommand>();
        foreach (var e in doc.RootElement.EnumerateArray())
        {
            var name = e.GetProperty("n").GetString();
            if (string.IsNullOrEmpty(name)) continue;
            result.Add(new PsCommand(name, e.GetProperty("t").GetString() ?? "", e.GetProperty("m").GetString() ?? ""));
        }
        return result.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static async Task<PsCommandHelp> HelpAsync(string pwsh, string name, CancellationToken ct = default)
    {
        var n = name.Replace("'", "''");
        var script =
            "$ErrorActionPreference='SilentlyContinue';" +
            $"$n='{n}';" +
            "$h=Get-Help $n | Select-Object -First 1;" +
            "$ex=@($h.Examples.Example | ForEach-Object { (([string]$_.Code) + \"`n\" + ($_.Remarks | Out-String)).Trim() });" +
            "[pscustomobject]@{ syntax=((Get-Command $n -Syntax) -join \"`n\"); synopsis=[string]$h.Synopsis; description=(($h.Description | Out-String).Trim()); examples=$ex } | ConvertTo-Json -Compress -Depth 4";

        var json = await RunAsync(pwsh, script, ct);
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        string S(string p) => r.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : "";

        var examples = new List<string>();
        if (r.TryGetProperty("examples", out var ex))
        {
            if (ex.ValueKind == JsonValueKind.Array)
                examples.AddRange(ex.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0));
            else if (ex.ValueKind == JsonValueKind.String && ex.GetString() is { Length: > 0 } single)
                examples.Add(single);
        }
        return new PsCommandHelp(S("syntax"), S("synopsis"), S("description"), examples);
    }

    static async Task<string> RunAsync(string pwsh, string script, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(pwsh)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        foreach (var a in new[] { "-NoProfile", "-NonInteractive", "-Command", script }) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Could not start PowerShell.");
        p.StandardInput.Close();
        var stderr = p.StandardError.ReadToEndAsync(ct);
        var stdout = await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        if (string.IsNullOrWhiteSpace(stdout))
            throw new InvalidOperationException((await stderr).Trim() is { Length: > 0 } err ? err : "PowerShell returned no data.");
        return stdout;
    }
}
