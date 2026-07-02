#load "local:?path=build/*.cake"
#tool nuget:?package=WiX&version=3.11.2

///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// SCRIPT
///////////////////////////////////////////////////////////////////////////////

Task("Create-Zip-Packages")
    .IsDependentOn("DotNetBuild")
    .IsDependeeOf("Package")
    .Does(async () =>
{
    EnsureDirectoryExists(BuildParameters.Paths.Directories.ChocolateyPackages);

    var outputFile = string.Format(
        "{0}/test-console.v{1}.zip",
        MakeAbsolute(new DirectoryPath(BuildParameters.Paths.Directories.ChocolateyPackages.FullPath)),
        BuildParameters.Version.SemVersion
    );

    Zip(BuildParameters.Paths.Directories.PublishedApplications.Combine("test-console"), outputFile);

    if (FileExists(outputFile))
    {
        BuildParameters.BuildProvider.UploadArtifact(outputFile);
        await BuildParameters.PublishProvider.AddArtifactAsync(ArtifactType.Other, outputFile);
    }
});

Task("Prepare-Chocolatey-Packages")
    .IsDependeeOf("Create-Chocolatey-Packages")
    .IsDependentOn("Copy-Nuspec-Folders")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping because not running on Windows")
    .WithCriteria(() => BuildParameters.ShouldRunChocolatey, "Skipping because execution of Chocolatey has been disabled")
    .Does(() =>
    {
        CleanDirectory(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/test-console/tools");
        CopyFiles(GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/test-console/net48/*"), BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/test-console/tools/", true);
    });

Task("Prepare-NuGet-Packages")
    .IsDependeeOf("Create-NuGet-Packages")
    .Does(() =>
    {
        CleanDirectory(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/test-library.lib/lib/net48");
        CopyFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/test-library/net48/*", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/test-library.lib/lib/net48");
    });

///////////////////////////////////////////////////////////////////////////////
// RECIPE SCRIPT
///////////////////////////////////////////////////////////////////////////////

Environment.SetVariableNames();

BuildParameters.AddGitHubPublishProvider(Context);

BuildParameters.SetParameters(context: Context,
                            buildSystem: BuildSystem,
                            sourceDirectoryPath: "./src",
                            solutionFilePath: "./src/testing.sln",
                            solutionDirectoryPath: "./src",
                            title: "Test App",
                            repositoryOwner: "AdmiringWorm",
                            repositoryName: "recipe-test",
                            productName: "Test App",
                            productDescription: "chocolatey is a product of Chocolatey Software, Inc. - All Rights Reserved.",
                            productCopyright: string.Format("Copyright © 2017 - {0} Chocolatey Software, Inc. Copyright © 2011 - 2017, RealDimensions Software, LLC - All Rights Reserved.", DateTime.Now.Year),
                            shouldStrongNameSignDependentAssemblies: false,
                            treatWarningsAsErrors: false,
                            preferDotNetGlobalToolUsage: !IsRunningOnWindows(),
                            shouldBuildMsi: false,
                            msiUsedWithinNupkg: false,
                            shouldAuthenticodeSignMsis: false,
                            shouldRunNuGet: IsRunningOnWindows(),
                            shouldAuthenticodeSignPowerShellScripts: IsRunningOnWindows(),
                            shouldPublishAwsLambdas: false,
                            shouldRunInspectCode: false,
                            shouldRunPSScriptAnalyzer: false,
                            getProjectsToPack: () => GetFiles("./src/test-library/*.csproj"),
                            shouldRunDotNetPack: true);

ToolSettings.SetToolSettings(context: Context);

BuildParameters.PrintParameters(Context);

Build.RunDotNet();
