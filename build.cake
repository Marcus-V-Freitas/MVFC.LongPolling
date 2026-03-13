Task("Default")
    .IsDependentOn("Test-Coverage")
    .Does(() =>
{
    Information("Build com Cake iniciado!");
});

Task("Clean")
    .Does(() =>
{
    Information("Limpando pastas de resultados e relatórios...");
    CleanDirectory("./coverage");
    CleanDirectory("./CoverageReport");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    Information("Restaurando pacotes...");
    DotNetRestore("MVFC.LongPolling.slnx");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Information("Build Release...");
    DotNetBuild("MVFC.LongPolling.slnx", new DotNetBuildSettings
    {
        Configuration = "Release",
        NoRestore = true
    });
});

Task("Test-Coverage")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testProject = "./tests/MVFC.LongPolling.Tests/MVFC.LongPolling.Tests.csproj";
    var resultsDir  = "./coverage";
    var reportDir   = "./CoverageReport";

    Information("Executando testes com cobertura...");
    DotNetTest(testProject, new DotNetTestSettings
    {
        Configuration = "Release",
        NoBuild = true,
        ResultsDirectory = resultsDir,
        ArgumentCustomization = args => args
            .Append("--collect:\"XPlat Code Coverage\"")
            .Append("--settings coverage.runsettings")
            .Append("--logger \"trx;LogFileName=test-results.trx\"")
    });

    var reports = GetFiles("./coverage/**/coverage.cobertura.xml");
    if (reports == null || reports.Count == 0)
    {
        Warning("Nenhum arquivo de cobertura encontrado.");
        return;
    }

    var reportGeneratorExe    = "./tools/reportgenerator";
    var reportGeneratorExeWin = "./tools/reportgenerator.exe";
    if (!FileExists(reportGeneratorExe) && !FileExists(reportGeneratorExeWin))
    {
        Information("Instalando ReportGenerator em ./tools...");
        StartProcess("dotnet", "tool install --tool-path ./tools dotnet-reportgenerator-globaltool");
    }

    var reportArgs = string.Join(";", reports.Select(f => f.FullPath));
    var rgPath     = FileExists(reportGeneratorExeWin) ? reportGeneratorExeWin : reportGeneratorExe;

    Information("Gerando relatório HTML...");
    StartProcess(rgPath, $"-reports:\"{reportArgs}\" -targetdir:\"{reportDir}\" -reporttypes:\"Html;Cobertura;MarkdownSummaryGithub\" -assemblyfilters:\"+MVFC.LongPolling*\" -classfilters:\"-*.Tests.*;-*.Playground.*\"");
    Information($"Relatório gerado em: {reportDir}");
});

RunTarget("Default");
