using LockCat.SetupCommon;
using System.IO;
using System.Reflection;

namespace LockCat.Installer;

internal sealed class BundledInstallerPackage : IDisposable
{
    private const string ResourcePrefix = "LockCatBundle/";
    private readonly string? _temporaryDirectory;

    private BundledInstallerPackage(string payloadDirectory, string uninstallerPath, string? temporaryDirectory)
    {
        PayloadDirectory = payloadDirectory;
        UninstallerPath = uninstallerPath;
        _temporaryDirectory = temporaryDirectory;
    }

    public string PayloadDirectory { get; }
    public string UninstallerPath { get; }

    public static BundledInstallerPackage Create()
    {
        string externalPayload = Path.Combine(AppContext.BaseDirectory, "Payload");
        string externalUninstaller = Path.Combine(AppContext.BaseDirectory, SetupOperations.UninstallerExecutable);
        if (Directory.Exists(externalPayload) && File.Exists(externalUninstaller))
        {
            return new BundledInstallerPackage(externalPayload, externalUninstaller, temporaryDirectory: null);
        }

        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] resources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .ToArray();
        if (resources.Length == 0)
        {
            throw new DirectoryNotFoundException($"Payload directory not found: {externalPayload}");
        }

        string tempDirectory = Path.Combine(Path.GetTempPath(), "LockCatInstaller", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        foreach (string resourceName in resources)
        {
            string relative = resourceName[ResourcePrefix.Length..].Replace('/', Path.DirectorySeparatorChar);
            string destination = SafeCombine(tempDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using Stream source = assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException("Embedded installer resource was not found.", resourceName);
            using FileStream target = File.Create(destination);
            source.CopyTo(target);
        }

        string payload = Path.Combine(tempDirectory, "Payload");
        string uninstaller = Path.Combine(tempDirectory, SetupOperations.UninstallerExecutable);
        if (!Directory.Exists(payload) || !File.Exists(uninstaller))
        {
            throw new DirectoryNotFoundException("Embedded LockCat installer payload is incomplete.");
        }

        return new BundledInstallerPackage(payload, uninstaller, tempDirectory);
    }

    public void Dispose()
    {
        if (_temporaryDirectory is null || !Directory.Exists(_temporaryDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
        catch
        {
            // Temporary extraction failures should not block a successful install.
        }
    }

    private static string SafeCombine(string root, string relativePath)
    {
        string fullRoot = Path.GetFullPath(root);
        string fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
        if (!fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Embedded installer resource path is invalid.");
        }

        return fullPath;
    }
}
