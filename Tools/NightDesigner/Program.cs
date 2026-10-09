using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using GhostHotel.Model.Content;

// NightDesigner: turns hand-written night designs into validated nights.
//   dotnet run --project Tools/NightDesigner              # all design files
//   dotnet run --project Tools/NightDesigner -- 14 15     # only these night numbers
// Each night is generated with seed = number × 7919, so output is stable until the design changes.
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var content = Path.Combine(root, "Assets", "_Game", "Content");
var json = new JsonSerializerOptions
{
    IncludeFields = true,
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
};

var guests = JsonSerializer.Deserialize<GuestFile>(File.ReadAllText(Path.Combine(content, "guests.json")), json).guests;
var nightsPath = Path.Combine(content, "nights.json");
var nights = JsonSerializer.Deserialize<NightFile>(File.ReadAllText(nightsPath), json).nights.ToDictionary(n => n.number);
int budgetArg = Array.IndexOf(args, "--budget");
long budgetMs = budgetArg >= 0 && budgetArg + 1 < args.Length && long.TryParse(args[budgetArg + 1], out var bs) ? bs * 1000 : 120_000;
var only = new HashSet<int>(args.Where((a, i) => i != budgetArg + 1 || budgetArg < 0)
    .Select(a => int.TryParse(a, out var n) ? n : -1).Where(n => n > 0));

// --explain N: show the best arrangement for a few candidate layouts, to see which guests can't be satisfied.
if (args.Contains("--explain"))
{
    foreach (var file in Directory.GetFiles(Path.Combine(content, "Design"), "*.json"))
    foreach (var d in JsonSerializer.Deserialize<DesignFile>(File.ReadAllText(file), json).nights)
    {
        if (!only.Contains(d.number)) continue;
        var probe = NightGenerator.Generate(new DesignNight
        {
            number = d.number, title = d.title, floors = d.floors, roomsPerFloor = d.roomsPerFloor, guests = d.guests,
            prePlaced = d.prePlaced, riddleGuest = d.riddleGuest, tagPool = d.tagPool, minTags = d.minTags, maxTags = d.maxTags,
            minPerfect = 1, maxPerfect = 100000,
        }, guests, seed: 1, maxAttempts: 300);
        Console.WriteLine($"Night {d.number} without midnight: {(probe.Ok ? "solvable" : "UNSOLVABLE")} — {probe.Report}");
        if (!probe.Ok)
        {
            var h = new ContentFactory(guests).Night(new NightData
            {
                number = d.number, floors = d.floors, roomsPerFloor = d.roomsPerFloor, guests = d.guests, prePlaced = d.prePlaced,
                rooms = d.tagPool.SelectMany((t, i) => Enumerable.Range(0, d.floors * d.roomsPerFloor)
                    .Where(c => c % d.tagPool.Length == i).Take(2)
                    .Select(c => new RoomData { room = ((c / d.roomsPerFloor + 1) * 100 + c % d.roomsPerFloor + 1).ToString(), tags = new[] { t } })).ToArray(),
            });
            Console.WriteLine("  " + GhostHotel.Model.NightSolver.Analyse(h, 2_000_000, 2_000_000));
        }
    }
    return 0;
}

int ok = 0, failed = 0;
foreach (var file in Directory.GetFiles(Path.Combine(content, "Design"), "*.json").OrderBy(f => f))
{
    var design = JsonSerializer.Deserialize<DesignFile>(File.ReadAllText(file), json);
    Console.WriteLine($"== {Path.GetFileName(file)}: {design.nights.Length} nights");
    foreach (var d in design.nights)
    {
        if (only.Count > 0 && !only.Contains(d.number)) continue;
        if (d.handmade)
        {
            nights[d.number] = Strip(d);
            Console.WriteLine($"   Night {d.number,2} {d.title}: handmade");
            ok++;
            continue;
        }
        var sw = Stopwatch.StartNew();
        Console.Write($"   Night {d.number,2} {d.title}: ");
        Console.Out.Flush();
        var r = NightGenerator.Generate(d, guests, seed: d.number * 7919, maxAttempts: 3000, timeBudgetMs: budgetMs);
        if (r.Ok)
        {
            nights[d.number] = r.Night;
            ok++;
            Console.WriteLine($"OK after {r.Attempts} tries, {sw.ElapsedMilliseconds} ms. {r.Report}");
        }
        else
        {
            failed++;
            Console.WriteLine($"FAILED after {sw.ElapsedMilliseconds} ms. {r.Report}");
        }
    }
}

var merged = new NightFile { nights = nights.Values.OrderBy(n => n.number).ToArray() };
File.WriteAllText(nightsPath, JsonSerializer.Serialize(merged, json) + "\n");
Console.WriteLine($"\n{ok} nights written, {failed} failed → {nightsPath}");
return failed == 0 ? 0 : 1;

// A handmade design night becomes a plain NightData (drops the generator-only fields).
static NightData Strip(DesignNight d) => new NightData
{
    number = d.number, title = d.title, floors = d.floors, roomsPerFloor = d.roomsPerFloor, rooms = d.rooms,
    guests = d.guests, prePlaced = d.prePlaced, swapTokens = d.swapTokens, midnight = d.midnight, intro = d.intro,
    tutorial = d.tutorial, lockRenovations = d.lockRenovations, ledgerPage = d.ledgerPage, riddleGuest = d.riddleGuest,
    vaneOffer = d.vaneOffer, vaneOfferEctoplasm = d.vaneOfferEctoplasm,
};
