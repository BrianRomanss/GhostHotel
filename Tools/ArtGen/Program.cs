using System;
using System.Diagnostics;
using System.IO;
using ChuchuGames.ProcArt;
using GhostHotel.EditorTools.ArtGen;

// Usage: dotnet run --project Tools/ArtGen [-- --sheet <contact-sheet.png>] [--no-audio]
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var art = Path.Combine(root, "Assets", "_Game", "Art", "Generated");
var audio = Path.Combine(root, "Assets", "_Game", "Audio", "Generated");
var sw = Stopwatch.StartNew();
var files = GhostHotelArt.GenerateAll(art);
Console.WriteLine($"{files.Count} images → {art} ({sw.ElapsedMilliseconds} ms)");
if (Array.IndexOf(args, "--no-audio") < 0)
    Console.WriteLine($"{GhostHotelAudio.GenerateAll(audio).Count} sounds → {audio}");

int sheetArg = Array.IndexOf(args, "--sheet");
if (sheetArg >= 0)
{
    // Contact sheet: every ghost/portrait/icon on one image for quick review.
    var sheet = new Raster(1600, 1180);
    sheet.Clear(Rgba.Hex("#1B1B3A"));
    int x = 10, y = 10;
    void Put(Raster r, int size)
    {
        if (x + size > sheet.Width) { x = 10; y += size + 10; }
        var scaled = r.Width == size ? r : r.Downscale(size, size);
        sheet.Draw(scaled, x, y);
        x += size + 10;
    }
    foreach (var t in GhostHotelArt.Tints.Keys) foreach (var e in GhostHotelArt.Expressions) Put(GhostHotelArt.Ghost(t, e), 128);
    x = 10; y += 138;
    foreach (var t in GhostHotelArt.Tints.Keys) Put(GhostHotelArt.Portrait(t, "neutral"), 128);
    x = 10; y += 138;
    foreach (var tag in GhostHotelArt.TagIds) Put(GhostHotelArt.TagIcon(tag), 128);
    x = 10; y += 138;
    Put(GhostHotelArt.RoomStage(), 256);
    Png.Write(args[sheetArg + 1], sheet);
    Console.WriteLine($"sheet → {args[sheetArg + 1]}");
}
