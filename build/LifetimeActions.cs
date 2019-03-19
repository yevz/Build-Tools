using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Core;
using DefiantCode.Cake.Frosting;
using DefiantCode.Cake.Frosting.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

public class LifetimeActions : ILifetimeActions
{
    public void AfterSetup(DotNetCoreContext context)
    {
    }

    public void AfterTeardown(DotNetCoreContext context, ITeardownContext info)
    {
    }

    public void BeforeSetup(DotNetCoreContext context)
    {
        //Disables git version and adds assemblyVersion and packageVersion arguments
        if (context.HasArgument("assemblyVersion"))
        {
            var av = context.Argument<string>("assemblyVersion");
            context.Information("Using assemblyVersion: {0}", av);
            var pv = context.Argument("packageVersion", av);
            context.Information("Using packageVersion: {0}", pv);
            context.DisableGitVersion = true;
            context.Information("Disabled GitVersion");
            context.BuildVersion = new BuildVersion(av, pv);
            context.Information("Using BuildVersion: {0}", context.BuildVersion);
        }

        //add octopus url property
        context.AddProperty(new KeyValuePair<string, object>(Constants.Ocotpus.ServerUrlPropertyName, context.Argument(Constants.Ocotpus.ServerUrlArgumentName, string.Empty)));
        //add octopus apikey property
        context.AddProperty(new KeyValuePair<string, object>(Constants.Ocotpus.ApiKeyPropertyName, context.Argument(Constants.Ocotpus.ApiKeyArgumentName, string.Empty)));
        //add octopus project property
        context.AddProperty(new KeyValuePair<string, object>(Constants.Ocotpus.ProjectPropertyName, context.Argument(Constants.Ocotpus.ProjectArgumentName, string.Empty)));
        //add publish projects property
        context.AddProperty(new KeyValuePair<string, object>(Constants.Ocotpus.PublishProjectsPropertyName, context.Argument(Constants.Ocotpus.PublishProjectsArgumentName, string.Empty)));


        //install octopus client
        try
        {
            context.InstallDotNetTool("Octopus.DotNet.Cli", "4.41.2");
        }
        catch (Exception)
        {
            context.Information("Octopus.DotNet.Cli::4.41.2 is already installed");
        }

    }

    public void BeforeTeardown(DotNetCoreContext context, ITeardownContext info)
    {
    }
}
