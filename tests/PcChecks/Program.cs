using MphRead;
using MphRead.Entities;
using MphRead.Text;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

int checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    Console.WriteLine("PASS " + description);
    checks++;
}

string folder = Path.Combine(Path.GetTempPath(), "hunters-pc-check-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
string path = Path.Combine(folder, "profile.json");
try
{
    string homeDirectory = Path.Combine(folder, "home");
    string xdgDirectory = Path.Combine(folder, "xdg-data");
    string defaultDataDirectory = Path.Combine(homeDirectory, ".local", "share", "hunterspc");
    Check(PcStorage.GetDataDirectory(true, folder, homeDirectory, null, null) == defaultDataDirectory,
        "Linux defaults to user data storage outside the application");
    Check(PcStorage.GetDataDirectory(true, folder, homeDirectory, xdgDirectory, null) == Path.Combine(xdgDirectory, "hunterspc"),
        "Linux respects absolute XDG_DATA_HOME");
    Check(PcStorage.GetDataDirectory(true, folder, homeDirectory, "relative", null) == defaultDataDirectory,
        "Relative XDG_DATA_HOME falls back to the standard directory");
    Check(PcStorage.GetDataDirectory(false, folder, folder, null, null) == folder,
        "Windows retains portable application storage");
    Check(PcStorage.GetDataDirectory(true, folder, homeDirectory, null, folder) == folder,
        "Explicit data directory isolates test installations");
    Check(Paths.Combine("models", @"sub\model.bin") == Path.Combine("models", "sub", "model.bin"),
        "ROM asset paths use native separators");
    Check(PcProfile.Load(path).MouseSensitivity == 1, "Missing profile has usable defaults");
    File.WriteAllText(path, "{ broken");
    Check(PcProfile.Load(path).MouseSensitivity == 1, "Broken JSON does not block startup");
    var profile = new PcProfile { MouseSensitivity = 12, SoundVolume = -1, MusicVolume = float.NaN, InvertY = true };
    profile.Save(path);
    profile = PcProfile.Load(path);
    Check(profile.MouseSensitivity == 4 && profile.SoundVolume == 0 && profile.MusicVolume == 0.5f && profile.InvertY,
        "Profile validates limits and round trips persistent controls");
    Check(!File.Exists(path + ".tmp"), "Atomic profile save leaves no temporary file");
    foreach (var size in new[] { new Vector2i(1280, 720), new Vector2i(1920, 1080), new Vector2i(1024, 768), new Vector2i(2560, 1080) })
    {
        float scale = Math.Min(size.X / 1280f, size.Y / 720f);
        Vector2 original = new(300, 310);
        Vector2 pixel = original * scale + new Vector2((size.X - 1280 * scale) / 2, (size.Y - 720 * scale) / 2);
        Vector2 actual = PcLayout.ToCanvas(pixel, size);
        Check((actual - original).Length < 0.01 && PcLayout.HitRow(actual, 4) == 1, $"Click hits rendered row at {size}");
    }
    Check(PcLayout.HitRow(new Vector2(90, 278), 4) == -1, "Gap between buttons is not clickable");
    Check(PcLayout.HitRow(new Vector2(10, 250), 4) == -1, "Outside panel is not clickable");
    Check(PcLayout.HitRow(new Vector2(90, 600), 7) == 6, "Last settings row stays reachable");
    var controls = PlayerControls.GetDefault();
    Check(controls.MoveUp.Key == Keys.W && controls.MoveLeft.Key == Keys.A && controls.MoveDown.Key == Keys.S && controls.MoveRight.Key == Keys.D,
        "Walking and strafing use WASD");
    Check(controls.Shoot.MouseButton == MouseButton.Left && controls.Zoom.MouseButton == MouseButton.Right && controls.MouseAim,
        "Fire, zoom and aiming use the mouse");
    Check(controls.Morph.Key == Keys.C && controls.ScanVisor.Key == Keys.E && controls.Scan.Key == Keys.Q,
        "Touch actions have keyboard bindings");
    bool rejected = false;
    try { PcRom.Validate(new byte[512]); } catch (InvalidDataException) { rejected = true; }
    Check(rejected, "Truncated or unrelated ROM is rejected before extraction");
    var invalidRom = new byte[64 * 1024 * 1024];
    "AMHE"u8.CopyTo(invalidRom.AsSpan(12));
    rejected = false;
    try { PcRom.Validate(invalidRom); } catch (InvalidDataException) { rejected = true; }
    Check(rejected, "A forged game header is rejected by SHA-256");
    "AMHP"u8.CopyTo(invalidRom.AsSpan(12));
    invalidRom[30] = 1;
    rejected = false;
    try { PcRom.Validate(invalidRom); } catch (InvalidDataException) { rejected = true; }
    Check(rejected, "A forged European revision is also rejected by SHA-256");
    var scans = new[]
    {
        new StringTableEntry("0000", '\0', "Equipment", "Known equipment description", 0, 'E'),
        new StringTableEntry("0001", '\0', "Secret lore", "Must remain hidden", 0, 'L'),
        new StringTableEntry("0002", '\0', "Bioform", "Scanned bioform", 0, 'B'),
        new StringTableEntry("0003", '\0', "Unused", "Not a journal category", 0, 'X')
    };
    var discovered = PcJournalCatalog.Read(scans, id => id != 1);
    Check(discovered.Count == 2 && discovered.All(e => e.ScanId != 1), "Journal hides undiscovered entries and unused categories");
    Check(discovered[1].ScanId == 2 && discovered[1].Body == "Scanned bioform", "Journal preserves original scan IDs and ROM descriptions");
    Check(PcJournalCatalog.Read(scans, _ => false).Count == 0, "Empty journal has no fabricated discoveries");
    var wrapped = PcText.Wrap("FIRST PARAGRAPH\n\nSUPERCALIFRAGILISTIC SECOND PARAGRAPH", 12);
    Check(wrapped.All(line => line.Length <= 12) && wrapped.Contains(""), "Journal wraps long words and preserves paragraph breaks");
    Check(PcText.Normalize("ÁREA\r\n\tTESTE\u0001") == "AREA\n TESTE", "ROM text is normalized without renderer control characters");
    Check(PcText.Ellipsis("LONG TITLE", 7) == "LONG...", "Room and scan titles stay inside the list width");
    var floor = new PcMapPolygon(new[] { new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(10, 0, 10), new Vector3(0, 0, 10) }, true);
    var upper = new PcMapPolygon(new[] { new Vector3(0, 40, 0), new Vector3(10, 40, 10), new Vector3(0, 40, 10) }, true);
    var geometry = new PcMapGeometry(new[] { floor, upper });
    var mapView = new PcMapView(geometry);
    Check(geometry.Min == Vector2.Zero && geometry.Max == new Vector2(10), "Map bounds come from room geometry");
    Check(floor.OnLevel(4) && !upper.OnLevel(4), "Map height filter separates distant floors");
    Vector3 worldPoint = new(3, 0, 7);
    Vector2 projected = mapView.Project(worldPoint);
    Check((mapView.Unproject(projected) - new Vector2(3, 7)).Length < 0.001f, "Map projection round trips world coordinates");
    mapView.Pan(new Vector2(24, 0));
    Check(mapView.Project(worldPoint).X < projected.X, "Panning changes map position without altering world geometry");
    mapView.ZoomBy(100);
    Check(mapView.Zoom == 8, "Map zoom has an upper bound");
    mapView.ZoomBy(0.0001f);
    Check(mapView.Zoom == 0.5f, "Map zoom has a lower bound");
    var emptyView = new PcMapView(new PcMapGeometry(Array.Empty<PcMapPolygon>()));
    Check(float.IsFinite(emptyView.Scale), "Empty map geometry does not divide by zero");
    Check(Metadata.GetRoomById(-1, noThrow: true) == null, "An empty campaign checkpoint safely falls back to the first room");
    Console.WriteLine($"{checks} checks passed.");
}
finally
{
    // Only the two known files created by this check, no recursive removal.
    if (File.Exists(path)) File.Delete(path);
    if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
    Directory.Delete(folder);
}
