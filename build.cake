#tool "nuget:?package=OpenCover"
#tool "nuget:?package=NUnit.ConsoleRunner"
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=JetBrains.dotCover.CommandLineTools"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
var target = Argument("target", "DotCover");
var configuration = Argument("configuration", "Debug");
var filter = Argument("filter", "**/*Test.dll");
var targetDirectory = Argument("targetDirectory", string.Empty);
var solutionFile = Argument("solutionFile", string.Empty);
var workingDirectory = Argument("workingDirectory", (Directory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + Directory("CodeCoverage")).Path.FullPath);
var reportName = Argument("reportName", "index");

var reportTypes = "Html";
var testResultsFileName = File("results.nunit.xml");
var openCoverFileName = File("results.opencover.html");
var dotCoverFileName = File("results.dotcover.html");
var testResultFilePath = Directory(workingDirectory) + testResultsFileName;
var openCoverFilePath = Directory(workingDirectory) + openCoverFileName;
var dotCoverFilePath = Directory(workingDirectory) + dotCoverFileName;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx => {
    // Executed BEFORE the first task.
    Information("Running tasks...");
});

Teardown(ctx => {
    // Executed AFTER the last task.
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// FUNCTIONS
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() => {
        if (!DirectoryExists(workingDirectory))
            CreateDirectory(workingDirectory);
        else
            CleanDirectory(workingDirectory);
});

Task("Build")
    .IsDependentOn("Clean")
    .Does(() => {
        var settings = new MSBuildSettings {
            Verbosity = Verbosity.Minimal,
            Configuration = configuration
        };

        MSBuild(solutionFile, settings);
});

Task("OpenCover")
    .IsDependentOn("Build")
    .Does(() => {
        var testAssemblies = GetFiles(Directory(targetDirectory) + File(filter));
        OpenCover(tool => {
            tool.NUnit3(testAssemblies,
                new NUnit3Settings {
                    ShadowCopy = false,
                    NoHeader = true,
                    WorkingDirectory = workingDirectory,
                    Results = new[] { new NUnit3Result { FileName = testResultFilePath }}
                });
            },
            openCoverFilePath,
            new OpenCoverSettings {
                TargetDirectory = targetDirectory,
                WorkingDirectory = workingDirectory
            }
            .WithFilter("+[*]*")
            .WithFilter("-[nunit*]*")
        );
});

Task("DotCover")
    .IsDependentOn("Build")
    .ContinueOnError()
    .Does(() => {
        var testAssemblies = GetFiles(Directory(targetDirectory) + File("**/*Test.dll"));
        DotCoverCover(tool => {
            tool.NUnit3(testAssemblies,
                new NUnit3Settings {
                    ShadowCopy = false,
                    NoHeader = true,
                    WorkingDirectory = workingDirectory,
                    Results = new[] { new NUnit3Result { FileName = testResultFilePath }}
            });
        },
        dotCoverFilePath,
        new DotCoverCoverSettings {
            ArgumentCustomization = args => args.Append($"--ReportType=HTML"),
            TargetWorkingDir = workingDirectory,
            WorkingDirectory = workingDirectory
        });
});

Task("Report_DotCover")
    .IsDependentOn("DotCover")
    .Does(() => {
        CopyFile(dotCoverFilePath, Directory(workingDirectory) + File(reportName + dotCoverFilePath.Path.GetExtension()));
});  

Task("Report_OpenCover")
    .IsDependentOn("OpenCover")
    .Does(() => {
        var settings = new ReportGeneratorSettings {
            ArgumentCustomization = args => args.Append($"-reportTypes:{reportTypes}"),
            SourceDirectories = new System.Collections.Generic.List<Cake.Core.IO.DirectoryPath> { Directory(targetDirectory) },
            WorkingDirectory = workingDirectory
        };
        ReportGenerator(openCoverFilePath, workingDirectory, settings);
});

RunTarget(target);