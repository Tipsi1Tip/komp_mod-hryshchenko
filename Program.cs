using System.Diagnostics;
using TvLine.Experiments;

string mode = args.Length > 0 ? args[0] : "all";
string outDir = args.Length > 1 ? args[1] : "results";
Directory.CreateDirectory(outDir);

const double tWarmup = 100_000;
const double tMod = 2_000_000;
var suite = new ExperimentSuite(outDir, tMod, tWarmup);
var sw = Stopwatch.StartNew();

switch (mode)
{
    case "protocol": suite.Protocol(); break;
    case "transient": suite.Transient(5, 2_000_000, 5_000); break;
    case "duration": suite.Duration(30, [100_000, 250_000, 500_000, 1_000_000, 2_000_000, 4_000_000]); break;
    case "pilot": suite.Pilot(20, 2.0, 0.001); break;
    default:
        suite.Protocol();
        suite.Verification(20);
        suite.Transient(5, 2_000_000, 5_000);
        suite.Duration(30, [100_000, 250_000, 500_000, 1_000_000, 2_000_000, 4_000_000]);
        int p = suite.Pilot(20, 2.0, 0.001);
        suite.Replications = Math.Max(20, p);
        suite.Baseline();
        suite.Regression([5.5, 5.75, 6.0, 6.25, 6.5, 7.0, 7.5, 8.0, 9.0]);
        suite.Modifications();
        suite.Proposals();
        break;
}
suite.SaveReport();
Console.WriteLine($"Час виконання: {sw.Elapsed.TotalSeconds:F1} с");
