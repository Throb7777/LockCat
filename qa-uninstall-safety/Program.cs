using LockCat.SetupCommon;
using System.Reflection;
using System.Text.Json;

string outputRoot = Path.Combine(Environment.CurrentDirectory, "qa-uninstall-safety-output");
if (Directory.Exists(outputRoot))
{
    Directory.Delete(outputRoot, recursive: true);
}

Directory.CreateDirectory(outputRoot);

TestInstallDirectoryNormalization(outputRoot);
TestManifestUninstallKeepsUserFiles(outputRoot);
TestLegacyUninstallKeepsUserFiles(outputRoot);
TestParentInstallDirectoryResolvesToLockCatChild(outputRoot);

Console.WriteLine("LockCat uninstall safety probe passed.");

static void TestInstallDirectoryNormalization(string outputRoot)
{
    string selected = Path.Combine(outputRoot, "DesktopLike");
    string normalized = SetupOperations.NormalizeInstallDirectoryForInstall(selected);
    Require(
        string.Equals(normalized, Path.Combine(selected, SetupOperations.ProductName), StringComparison.OrdinalIgnoreCase),
        "Installer should append a LockCat child folder when the selected path is a parent directory.");

    string alreadyProductFolder = Path.Combine(outputRoot, SetupOperations.ProductName);
    string unchanged = SetupOperations.NormalizeInstallDirectoryForInstall(alreadyProductFolder);
    Require(
        string.Equals(unchanged, alreadyProductFolder, StringComparison.OrdinalIgnoreCase),
        "Installer should keep an explicitly selected LockCat folder.");
}

static void TestManifestUninstallKeepsUserFiles(string outputRoot)
{
    string installDirectory = Path.Combine(outputRoot, "manifest-broad-folder");
    Directory.CreateDirectory(Path.Combine(installDirectory, "Assets", "Icons"));
    File.WriteAllText(Path.Combine(installDirectory, "user.md"), "must survive");
    File.WriteAllText(Path.Combine(installDirectory, "LockCat.exe"), "product");
    File.WriteAllText(Path.Combine(installDirectory, "Assets", "Icons", "LockCatUninstaller.ico"), "product icon");
    File.WriteAllText(Path.Combine(outputRoot, "outside.txt"), "outside");

    InstallManifest manifest = new()
    {
        ProductId = SetupOperations.ProductName,
        InstallDirectory = installDirectory,
        InstalledFiles =
        [
            "LockCat.exe",
            @"Assets\Icons\LockCatUninstaller.ico",
            "..\\outside.txt",
            SetupOperations.ManifestFile
        ],
        InstalledDirectories =
        [
            @"Assets\Icons",
            "Assets"
        ]
    };
    File.WriteAllText(
        Path.Combine(installDirectory, SetupOperations.ManifestFile),
        JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

    InvokeDeleteInstallDirectory(installDirectory);

    Require(File.Exists(Path.Combine(installDirectory, "user.md")), "Manifest uninstall deleted a user file.");
    Require(File.Exists(Path.Combine(outputRoot, "outside.txt")), "Manifest uninstall followed a path traversal entry.");
    Require(!File.Exists(Path.Combine(installDirectory, "LockCat.exe")), "Manifest uninstall did not delete product file.");
    Require(!File.Exists(Path.Combine(installDirectory, "Assets", "Icons", "LockCatUninstaller.ico")), "Manifest uninstall did not delete product asset.");
}

static void TestLegacyUninstallKeepsUserFiles(string outputRoot)
{
    string installDirectory = Path.Combine(outputRoot, "legacy-broad-folder");
    Directory.CreateDirectory(Path.Combine(installDirectory, "Assets", "Illustrations"));
    Directory.CreateDirectory(Path.Combine(installDirectory, "Assets", "Fonts", "FusionPixelFont-Upstream-LICENSE"));
    File.WriteAllText(Path.Combine(installDirectory, "user.docx"), "must survive");
    File.WriteAllText(Path.Combine(installDirectory, "Assets", "user-asset.txt"), "must survive");
    File.WriteAllText(Path.Combine(installDirectory, "LockCat.exe"), "product");
    File.WriteAllText(Path.Combine(installDirectory, "LockCat.dll"), "product");
    File.WriteAllText(Path.Combine(installDirectory, "Assets", "Illustrations", "install-welcome.png"), "product asset");
    File.WriteAllText(Path.Combine(installDirectory, "Assets", "Fonts", "FusionPixelFont-OFL.txt"), "product license");
    File.WriteAllText(Path.Combine(installDirectory, "Assets", "Fonts", "FusionPixelFont-Upstream-LICENSE", "ark-pixel.txt"), "product upstream license");

    InvokeDeleteInstallDirectory(installDirectory);

    Require(File.Exists(Path.Combine(installDirectory, "user.docx")), "Legacy uninstall deleted a user file.");
    Require(File.Exists(Path.Combine(installDirectory, "Assets", "user-asset.txt")), "Legacy uninstall deleted a user asset file.");
    Require(!File.Exists(Path.Combine(installDirectory, "LockCat.exe")), "Legacy uninstall did not delete LockCat.exe.");
    Require(!File.Exists(Path.Combine(installDirectory, "LockCat.dll")), "Legacy uninstall did not delete LockCat.dll.");
    Require(!File.Exists(Path.Combine(installDirectory, "Assets", "Illustrations", "install-welcome.png")), "Legacy uninstall did not delete known LockCat asset.");
    Require(!File.Exists(Path.Combine(installDirectory, "Assets", "Fonts", "FusionPixelFont-OFL.txt")), "Legacy uninstall did not delete known LockCat font license.");
    Require(!File.Exists(Path.Combine(installDirectory, "Assets", "Fonts", "FusionPixelFont-Upstream-LICENSE", "ark-pixel.txt")), "Legacy uninstall did not delete known LockCat upstream license.");
}

static void TestParentInstallDirectoryResolvesToLockCatChild(string outputRoot)
{
    string parentDirectory = Path.Combine(outputRoot, "parent-with-lockcat-child");
    string childDirectory = Path.Combine(parentDirectory, SetupOperations.ProductName);
    Directory.CreateDirectory(childDirectory);
    Directory.CreateDirectory(Path.Combine(parentDirectory, "System Volume Information"));
    File.WriteAllText(Path.Combine(parentDirectory, "user.pdf"), "must survive");
    File.WriteAllText(Path.Combine(parentDirectory, "System Volume Information", "protected.txt"), "must survive");
    File.WriteAllText(Path.Combine(childDirectory, "LockCat.exe"), "product");

    string resolved = SetupOperations.ResolveInstallDirectoryFromArgs(["--install-dir", parentDirectory]);
    Require(
        string.Equals(resolved, childDirectory, StringComparison.OrdinalIgnoreCase),
        "Uninstaller should resolve a parent install directory to its LockCat child folder.");

    InvokeDeleteInstallDirectory(resolved);
    Require(File.Exists(Path.Combine(parentDirectory, "user.pdf")), "Child-folder uninstall deleted a parent user file.");
    Require(File.Exists(Path.Combine(parentDirectory, "System Volume Information", "protected.txt")), "Child-folder uninstall enumerated or deleted a parent protected directory.");
    Require(!File.Exists(Path.Combine(childDirectory, "LockCat.exe")), "Child-folder uninstall did not delete product file.");
}

static void InvokeDeleteInstallDirectory(string installDirectory)
{
    MethodInfo method = typeof(SetupOperations).GetMethod(
        "DeleteInstallDirectory",
        BindingFlags.NonPublic | BindingFlags.Static) ?? throw new MissingMethodException("DeleteInstallDirectory");
    method.Invoke(null, [installDirectory]);
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
