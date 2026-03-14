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
    if (exitCode != 0) throw new Exception($"dotnet restore falhou ({exitCode})");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Information("Build Release...");
    var exitCode = StartProcess("dotnet", "build MVFC.LongPolling.slnx --configuration Release --no-restore");
    if (exitCode != 0) throw new Exception($"dotnet build falhou ({exitCode})");
});

Task("Test-Coverage")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testProject = "./tests/MVFC.LongPolling.Tests/MVFC.LongPolling.Tests.csproj";
    var resultsDir  = "./coverage";
    var reportDir   = "./CoverageReport";

    Information("Executando testes com cobertura...");
    var exitCode = StartProcess("dotnet", $"test \"{testProject}\" --configuration Release --no-build --collect:\"XPlat Code Coverage\" --results-directory \"{resultsDir}\" --settings coverage.runsettings --logger \"trx;LogFileName=test-results.trx\"");
    if (exitCode != 0) throw new Exception($"dotnet test falhou ({exitCode})");

    var reports = GetFiles("./coverage/**/coverage.cobertura.xml");
    if (reports == null || reports.Count == 0)
    {
        throw new Exception("Nenhum arquivo de cobertura encontrado após os testes.");
    }

    var reportGeneratorExe    = "./tools/reportgenerator";
    var reportGeneratorExeWin = "./tools/reportgenerator.exe";
    if (!FileExists(reportGeneratorExe) && !FileExists(reportGeneratorExeWin))
    {
        Information("Instalando ReportGenerator em ./tools...");
        var installCode = StartProcess("dotnet", "tool install --tool-path ./tools dotnet-reportgenerator-globaltool");
        if (installCode != 0) throw new Exception($"Instalação do ReportGenerator falhou ({installCode})");
    }

    var reportArgs = string.Join(";", reports.Select(f => f.FullPath));
    var rgPath     = FileExists(reportGeneratorExeWin) ? reportGeneratorExeWin : reportGeneratorExe;

    Information("Gerando relatório HTML...");
    var rgCode = StartProcess(rgPath, $"-reports:\"{reportArgs}\" -targetdir:\"{reportDir}\" -reporttypes:\"Html;Cobertura;MarkdownSummaryGithub\" -assemblyfilters:\"+MVFC.LongPolling*\" -classfilters:\"-*.Tests.*;-*.Playground.*\"");
    if (rgCode != 0) throw new Exception($"ReportGenerator falhou ({rgCode})");

    Information($"Relatório gerado em: {reportDir}");
});

RunTarget("Default");
