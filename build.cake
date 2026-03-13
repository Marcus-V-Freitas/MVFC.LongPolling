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
    var exitCode = StartProcess("dotnet", "restore MVFC.LongPolling.slnx");
    if (exitCode != 0) throw new Exception($"dotnet restore failed with exit code {exitCode}");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Information("Build Release...");
    var exitCode = StartProcess("dotnet", "build MVFC.LongPolling.slnx --configuration Release --no-restore");
    if (exitCode != 0) throw new Exception($"dotnet build failed with exit code {exitCode}");
});

Task("Test-Coverage")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testProject = "./tests/MVFC.LongPolling.Tests/MVFC.LongPolling.Tests.csproj";
    var resultsDir  = "./coverage";
    var reportDir   = "./CoverageReport";

    Information("Executando testes com cobertura...");
    var testExitCode = StartProcess("dotnet", $"test \"{testProject}\" --configuration Release --no-build --collect:\"XPlat Code Coverage\" --results-directory \"{resultsDir}\" --settings coverage.runsettings --logger \"trx;LogFileName=test-results.trx\"");
    if (testExitCode != 0) throw new Exception($"dotnet test failed with exit code {testExitCode}");

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
    var rgExitCode = StartProcess(rgPath, $"-reports:\"{reportArgs}\" -targetdir:\"{reportDir}\" -reporttypes:\"Html;Cobertura;MarkdownSummaryGithub\" -assemblyfilters:\"+MVFC.LongPolling*\" -classfilters:\"-*.Tests.*;-*.Playground.*\"");
    if (rgExitCode != 0) throw new Exception($"ReportGenerator failed with exit code {rgExitCode}");
    Information($"Relatório gerado em: {reportDir}");
});

RunTarget("Default");

