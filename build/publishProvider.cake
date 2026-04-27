BuildParameters.Tasks.PublishPublicArtifactsTask = Task("Publish-Public-Artifacts")
    .IsDependentOn("Build-MSI")
    .IsDependentOn("Package")
    .WithCriteria(() => BuildParameters.ShouldPublishPublicArtifacts, "Skipping because publishing of public artifacts is not enabled")
    .WithCriteria(() => !BuildParameters.IsLocalBuild || BuildParameters.ForceContinuousIntegration, "Skipping because this is a local build, and force isn't being applied")
    .WithCriteria(() => !BuildParameters.IsPullRequest, "Skipping because current build is from a Pull Request")
    .WithCriteria(() => BuildParameters.IsTagged, "Skipping because current commit is not tagged")
    .Does(() =>
    {
        var provider = BuildParameters.PublishProvider;

        provider.PublishArtifacts();
    })
    .OnError(exception =>
    {
        Error(exception.Message);
        Information("Publish-Public-Artifacts Task failed, but continuing with next Task...");
        // We only set publishing errors if this is a stable release, pre-releases may not have
        // any release notes associated, as such it is expected that the publishing may fail.
        // To allow public pre-releases in the future, we however still attempt to upload any artifacts.
        publishingError = BuildParameters.Version.MajorMinorPath == BuildParameters.Version.SemVersion;
    });

public enum PublishProviderType
{
    None,
    GitReleaseManager,
}

public interface IPublishProvider
{
    string Name { get; }

    void AddArtifact(params FilePath[] artifactPaths);

    void PublishArtifacts();
}

public static IPublishProvider GetPublishProvider(ICakeContext context, PublishProviderType providerType)
{
    switch (providerType)
    {
        case PublishProviderType.GitReleaseManager:
            return new GitReleaseManagerPublishProvider(context);

        default:
            return new NullPublishProvider(context);
    }
}

internal class NullPublishProvider : IPublishProvider
{
    private readonly ICakeContext _context;

    public NullPublishProvider(ICakeContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public string Name { get; } = "None";

    public void AddArtifact(params FilePath[] artifactPaths)
    {
        // Nothing to do, we aren't publishing anything in this case.
    }

    public void PublishArtifacts()
    {
        _context.Information("[NullPublishProvider] No artifacts possible to publish.");
    }
}
