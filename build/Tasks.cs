using Cake.Common.Diagnostics;
using Cake.Frosting;
using DefiantCode.Cake.Frosting;
using DefiantCode.Cake.Frosting.Tasks;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Core;
using System.Linq;
using System;

[TaskName("Default")]
public class DefaultTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
        context.Information("Default task run");
    }
}

[TaskName("build")]
[Dependency(typeof(DotNetCoreBuild))]
public class BuildTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
    }
}

[TaskName("clean")]
[Dependency(typeof(DotNetCoreClean))]
public class CleanTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
    }
}

[TaskName("restore")]
[Dependency(typeof(DotNetCoreRestore))]
public class RestoreTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
    }
}

[TaskName("pack")]
[Dependency(typeof(DotNetCorePack))]
public class PackTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
    }
}

[TaskName("publish")]
[Dependency(typeof(DotNetCorePublish))]
public class PublishTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
    }
}

[TaskName("push")]
[Dependency(typeof(DotNetCoreNugetPush))]
public class PushTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
    }
}

[TaskName("test")]
[Dependency(typeof(DotNetCoreRestore))]
[Dependency(typeof(DotNetCoreBuild))]
public class TestTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
        var projects = context.GetFiles("./**/*.Tests.csproj");
        foreach (var project in projects)
        {
            context.DotNetCoreTest(project.ToString(), new DotNetCoreTestSettings {
            Configuration = context.Configuration,
            NoBuild = true
        });
        }
    }
}

[TaskName("ci")]
[Dependency(typeof(DotNetCoreRestore))]
[Dependency(typeof(TestTask))]
[Dependency(typeof(DotNetCorePack))]
[Dependency(typeof(OctoPackTask))]
public class CiTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
    }
}

[TaskName("octo-pack")]
[Dependency(typeof(DotNetCorePublish))]
public class OctoPackTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
        var projectsArg = context.GetProperty<string>(Constants.Ocotpus.PublishProjectsPropertyName);
        var projectsToPublish = new string[]{};
        if(!string.IsNullOrEmpty(projectsArg))
            projectsToPublish = projectsArg.Split(',', StringSplitOptions.RemoveEmptyEntries);

        context.Information(projectsArg);            

        foreach (var p in context.Outputs.PublishedProjects)
        {
            context.Information(p.ProjectName);            
        }
        
        foreach (var project in context.Outputs.PublishedProjects.Where(x => projectsToPublish.Contains(x.ProjectName)))
        {
            //for some reason this alias wants a path to a csproj but it's never used, only the directory the file is in
            //so we create a filepath to the working directory and a non-existant csproj file
            var projectPath = context.Environment.WorkingDirectory.CombineWithFilePath("dummy.csproj");
            //use the msbuild packageId with a fallback to the project name
            var packageId = string.IsNullOrWhiteSpace(project.ProjectParserResult.NetCore.PackageId) ? project.ProjectName : project.ProjectParserResult.NetCore.PackageId;
            context.DotNetCoreTool(projectPath, $"octo pack --basePath {context.Artifacts.Combine(packageId)} --outFolder {context.Artifacts} --id {packageId} --version {context.BuildVersion.Version.FullSemVer} --format Zip --overwrite");
        }

    }
}

[TaskName("octo-push")]
[Dependency(typeof(OctoPackTask))]
public class OctoPushTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
        var octoServerUrl = context.GetProperty<string>(Constants.Ocotpus.ServerUrlPropertyName);
        var octoApiKey = context.GetProperty<string>(Constants.Ocotpus.ApiKeyPropertyName);
        var allPackages = string.Empty;

        foreach (var octoPackage in context.GetFiles(System.IO.Path.Combine(context.Artifacts.MakeAbsolute(context.Environment).FullPath, "*.zip")))
        {
            allPackages += $"--package={octoPackage} ";
        }

        var fullToolCommand = $"octo push {allPackages.TrimEnd()} --server={octoServerUrl} --apiKey={octoApiKey} --enableServiceMessages";
        //for some reason this alias wants a path to a csproj but it's never used, only the directory the file is in
        //so we create a filepath to the working directory and a non-existant csproj file
        var projectPath = context.Environment.WorkingDirectory.CombineWithFilePath("dummy.csproj");
        context.DotNetCoreTool(projectPath, fullToolCommand);

    }
}

[TaskName("octo-release")]
[Dependency(typeof(OctoPushTask))]
public class OctoReleaseTask : FrostingTask<DotNetCoreContext>
{
    public override void Run(DotNetCoreContext context)
    {
        var octoServerUrl = context.GetProperty<string>(Constants.Ocotpus.ServerUrlPropertyName);
        var octoApiKey = context.GetProperty<string>(Constants.Ocotpus.ApiKeyPropertyName);
        var octoProjectName = context.GetProperty<string>(Constants.Ocotpus.ProjectPropertyName);
        var allPackages = string.Empty;

        //foreach (var octoPackage in context.GetFiles(System.IO.Path.Combine(context.Artifacts.MakeAbsolute(context.Environment).FullPath, "*.zip")))
        //{
        
            var fullToolCommand = $"octo create-release --project={octoProjectName} --defaultpackageversion={context.BuildVersion.Version.FullSemVer} --version={context.BuildVersion.Version.FullSemVer} --server={octoServerUrl} --apiKey={octoApiKey} --enableServiceMessages";
            //for some reason this alias wants a path to a csproj but it's never used, only the directory the file is in
            //so we create a filepath to the working directory and a non-existant csproj file
            var projectPath = context.Environment.WorkingDirectory.CombineWithFilePath("dummy.csproj");
            context.DotNetCoreTool(projectPath, fullToolCommand);
        //}

        

    }
}