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
    CleanDirectory("./TestResults");
    CleanDirectory("./CoverageReport");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    Information("Restaurando pacotes dotnet...");
    StartProcess("dotnet", "restore");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Information("Build do solution (Release)...");
    StartProcess("dotnet", "build --configuration Release");
});

Task("Test-Coverage")
    .IsDependentOn("Build")
    .Does(() =>
{
    var solution = "./MVFC.LongPolling.slnx";
    var resultsDir = "./TestResults";
    var reportDir = "./CoverageReport";

    Information("Executando testes e coletando cobertura (coverlet.collector)...");
    StartProcess("dotnet", $"test \"{solution}\" --configuration Release --no-build --collect:\"XPlat Code Coverage\" --results-directory \"{resultsDir}\"");

    var reports = GetFiles("./TestResults/**/coverage.cobertura.xml");
    if (reports == null || reports.Count == 0)
    {
        Warning("Nenhum arquivo de cobertura encontrado em './TestResults'.");
        return;
    }

    var reportGeneratorExe = "./tools/reportgenerator";
    var reportGeneratorExeWin = "./tools/reportgenerator.exe";
    if (!FileExists(reportGeneratorExe) && !FileExists(reportGeneratorExeWin))
    {
        Information("Instalando dotnet-reportgenerator-globaltool em ./tools...");
        StartProcess("dotnet", "tool install --tool-path ./tools dotnet-reportgenerator-globaltool");
    }

    var reportArgs = string.Empty;
    foreach (var f in reports)
    {
        reportArgs = string.IsNullOrEmpty(reportArgs)
            ? f.FullPath
            : reportArgs + ";" + f.FullPath;
    }

    var rgPath = FileExists(reportGeneratorExeWin) ? reportGeneratorExeWin : reportGeneratorExe;
    StartProcess(rgPath, $"-reports:\"{reportArgs}\" -targetdir:\"{reportDir}\" -reporttypes:HtmlInline_AzurePipelines");
    Information($"Relatório gerado em: {reportDir}");
});

RunTarget("Default");
