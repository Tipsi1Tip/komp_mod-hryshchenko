using System.Globalization;
using System.Text;
using TvLine.Network;

namespace TvLine.Experiments;

public sealed class ExperimentSuite(string outDir, double tMod, double tWarmup)
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private readonly StringBuilder report = new();

    public double TMod { get; set; } = tMod;
    public double TWarmup { get; set; } = tWarmup;
    public int Replications { get; set; } = 20;

    private void Log(string s)
    {
        Console.WriteLine(s);
        report.AppendLine(s);
    }

    private void Csv(string file, string header, IEnumerable<string> rows) =>
        File.WriteAllLines(Path.Combine(outDir, file), new[] { header }.Concat(rows));

    private static string F(double v, int d = 4) => v.ToString("F" + d, Inv);

    public SimulationResult[] Series(ModelParameters p, int n, double? tMod = null, double? tWarm = null, int seed0 = 1)
    {
        double tm = tMod ?? TMod, tw = tWarm ?? TWarmup;
        return Enumerable.Range(seed0, n).AsParallel().AsOrdered()
            .Select(s => new TvLineNetwork(p, s).Run(tw + tm, tw)).ToArray();
    }

    public void Protocol(double t = 90)
    {
        var path = Path.Combine(outDir, "protocol.txt");
        using var w = new StreamWriter(path);
        w.WriteLine("Час, хв | Подія | K1 L1 | K2 L2 | N0 N1 N2 N3");
        var net = new TvLineNetwork(ModelParameters.Base, 7);
        net.EnableProtocol(w);
        net.Run(t);
        Log($"protocol: {net.Model.EventCount} подій до t={t}");
    }

    public void Verification(int n)
    {
        var cases = new (string Name, ModelParameters P)[]
        {
            ("Базові значення", ModelParameters.Base),
            ("Інтервал U(4,8)", ModelParameters.Base with { ArrivalMin = 4, ArrivalMax = 8 }),
            ("Інтервал U(5,9)", ModelParameters.Base with { ArrivalMin = 5, ArrivalMax = 9 }),
            ("Перевірка U(5,9)", ModelParameters.Base with { CheckMin = 5, CheckMax = 9 }),
            ("Контролерів 3", ModelParameters.Base with { Controllers = 3 }),
            ("Налаштування U(10,20)", ModelParameters.Base with { AdjustMin = 10, AdjustMax = 20 }),
            ("Настроювачів 2", ModelParameters.Base with { Adjusters = 2 }),
            ("Ймовірність 0,10", ModelParameters.Base with { PAdjust = 0.10 }),
            ("Ймовірність 0", ModelParameters.Base with { PAdjust = 0 }),
        };
        var rows = new List<string>();
        Log("== Верифікація ==");
        foreach (var (name, p) in cases)
        {
            var r = Series(p, n);
            var tb = Statistics.Describe(r.Select(x => x.MeanTime).ToArray());
            double rk = r.Average(x => x.RhoCheck), rn = r.Average(x => x.RhoAdjust);
            double thr = r.Average(x => x.Throughput), chk = r.Average(x => x.ChecksPerTv);
            double thrT = 60 / p.MeanArrival;
            rows.Add(string.Join(';', name, F(p.RhoCheck), F(rk), F(p.RhoAdjust), F(rn), F(thrT, 3), F(thr, 3),
                F(p.ChecksPerTv), F(chk), F(tb.Mean, 2), F(r.Average(x => x.QueueCheck), 3), F(r.Average(x => x.QueueAdjust), 3)));
            Log($"{name,-24} rk {F(p.RhoCheck)}/{F(rk)} rn {F(p.RhoAdjust)}/{F(rn)} thr {F(thrT, 3)}/{F(thr, 3)} chk {F(p.ChecksPerTv)}/{F(chk)} T {F(tb.Mean, 2)}");
        }
        Csv("verification.csv", "case;rk_theory;rk_model;rn_theory;rn_model;thr_theory;thr_model;chk_theory;chk_model;T;L1;L2", rows);

        var det = new TvLineNetwork(ModelParameters.Base with { Deterministic = true, PAdjust = 0 }, 1).Run(TMod);
        Log($"Детермінований (p=0): T={F(det.MeanTime)} (теор. 9), rk={F(det.RhoCheck)} (теор. {F(9.0 / 5.5 / 2)})");

        var exp = ModelParameters.Base with { Exponential = true, ArrivalMin = 5, ArrivalMax = 9 };
        var er = Series(exp, n);
        double lam = 1 / exp.MeanArrival, l1 = lam / 0.85, r1 = l1 * 9 / 2, r2 = 0.15 * l1 * 30;
        double L1 = 2 * r1 / (1 - r1 * r1), L2 = r2 / (1 - r2), Tt = (L1 + L2) / lam;
        var es = Statistics.Describe(er.Select(x => x.MeanTime).ToArray());
        double mL1 = er.Average(x => x.QueueCheck + x.RhoCheck * 2), mL2 = er.Average(x => x.QueueAdjust + x.RhoAdjust);
        Log($"Джексон a=7: rho1 {F(r1)}/{F(er.Average(x => x.RhoCheck))} rho2 {F(r2)}/{F(er.Average(x => x.RhoAdjust))} " +
            $"N1 {F(L1)}/{F(mL1)} N2 {F(L2)}/{F(mL2)} T {F(Tt, 3)}/{F(es.Mean, 3)}±{F(es.HalfWidth, 3)}");
        Csv("jackson.csv", "name;theory;model",
        [
            $"rho1;{F(r1)};{F(er.Average(x => x.RhoCheck))}",
            $"rho2;{F(r2)};{F(er.Average(x => x.RhoAdjust))}",
            $"N1;{F(L1)};{F(mL1)}",
            $"N2;{F(L2)};{F(mL2)}",
            $"T;{F(Tt, 3)};{F(es.Mean, 3)}",
            $"T_half;0;{F(es.HalfWidth, 3)}",
            $"det_T;9;{F(det.MeanTime)}",
            $"det_rk;{F(9.0 / 11)};{F(det.RhoCheck)}",
        ]);
    }

    public void Transient(int runs, double tTotal, double step)
    {
        Log("== Перехідний період ==");
        var series = Enumerable.Range(1, runs).AsParallel().AsOrdered().Select(seed =>
        {
            var net = new TvLineNetwork(ModelParameters.Base, 100 + seed);
            var pts = new List<(double T, double Tb, double Rk, double Rn)>();
            for (double t = step; t <= tTotal + 1e-9; t += step)
            {
                net.Model.Simulate(t);
                var r = net.Collect();
                pts.Add((t, r.MeanTime, r.RhoCheck, r.RhoAdjust));
            }
            return pts;
        }).ToArray();
        var rows = new List<string>();
        for (int i = 0; i < series[0].Count; i++)
        {
            var parts = new List<string> { F(series[0][i].T, 0) };
            foreach (var s in series) parts.AddRange([F(s[i].Tb, 3), F(s[i].Rk), F(s[i].Rn)]);
            rows.Add(string.Join(';', parts));
        }
        var header = "t;" + string.Join(';', Enumerable.Range(1, runs).Select(k => $"T{k};rk{k};rn{k}"));
        Csv("transient.csv", header, rows);
        foreach (double probe in new[] { 50000.0, 100000, 250000, 500000, 1000000, tTotal })
        {
            int idx = series[0].FindIndex(x => x.T >= probe - 1e-9);
            if (idx < 0) continue;
            var vals = series.Select(s => s[idx].Tb).ToArray();
            Log($"t={probe,8}: T̄ = [{string.Join(", ", vals.Select(v => F(v, 2)))}], розмах {F(vals.Max() - vals.Min(), 2)}; " +
                $"rk розмах {F(series.Max(s => s[idx].Rk) - series.Min(s => s[idx].Rk))}");
        }
    }

    public void Duration(int n, double[] lengths)
    {
        Log("== Тривалість моделювання ==");
        var rows = new List<string>();
        foreach (double len in lengths)
        {
            var r = Series(ModelParameters.Base, n, len, TWarmup, 500);
            var s = Statistics.Describe(r.Select(x => x.MeanTime).ToArray());
            var sk = Statistics.Describe(r.Select(x => x.RhoCheck).ToArray());
            var sn = Statistics.Describe(r.Select(x => x.RhoAdjust).ToArray());
            rows.Add(string.Join(';', F(len, 0), F(s.Mean, 3), F(s.Std, 3), F(sk.Std, 5), F(sn.Std, 5)));
            Log($"Tmod={len,9}: T̄={F(s.Mean, 2)} σ={F(s.Std, 3)} σρк={F(sk.Std, 5)} σρн={F(sn.Std, 5)}");
        }
        Csv("duration.csv", "tmod;mean;std;std_rk;std_rn", rows);
    }

    public int Pilot(int n, double eps, double epsRho)
    {
        Log("== Кількість прогонів ==");
        var r = Series(ModelParameters.Base, n, seed0: 1000);
        var s = Statistics.Describe(r.Select(x => x.MeanTime).ToArray());
        var sk = Statistics.Describe(r.Select(x => x.RhoCheck).ToArray());
        var sn = Statistics.Describe(r.Select(x => x.RhoAdjust).ToArray());
        int p = Statistics.Replications(s.Std, eps);
        int pk = Statistics.Replications(sk.Std, epsRho);
        int pn = Statistics.Replications(sn.Std, epsRho);
        Log($"σT={F(s.Std, 3)} -> p={p} (ε={eps}); σρк={F(sk.Std, 5)} -> p={pk}; σρн={F(sn.Std, 5)} -> p={pn} (ε={epsRho})");
        Csv("pilot.csv", "name;mean;std;eps;p",
        [
            $"T;{F(s.Mean, 3)};{F(s.Std, 4)};{eps};{p}",
            $"rk;{F(sk.Mean, 5)};{F(sk.Std, 5)};{epsRho};{pk}",
            $"rn;{F(sn.Mean, 5)};{F(sn.Std, 5)};{epsRho};{pn}",
        ]);
        Csv("pilot_runs.csv", "run;T;rk;rn", r.Select((x, i) => $"{i + 1};{F(x.MeanTime, 3)};{F(x.RhoCheck, 5)};{F(x.RhoAdjust, 5)}"));
        return Math.Max(p, Math.Max(pk, pn));
    }

    public SimulationResult[] Baseline()
    {
        Log($"== Базова модель, {Replications} прогонів ==");
        var r = Series(ModelParameters.Base, Replications);
        Csv("baseline_runs.csv", "run;T;rk;rn;L1;L2;W1;W2;thr;chk;N",
            r.Select((x, i) => string.Join(';', i + 1, F(x.MeanTime, 3), F(x.RhoCheck, 5), F(x.RhoAdjust, 5),
                F(x.QueueCheck, 3), F(x.QueueAdjust, 3), F(x.WaitCheck, 3), F(x.WaitAdjust, 3),
                F(x.Throughput, 3), F(x.ChecksPerTv, 4), F(x.MeanInSystem, 3))));
        var rows = new List<string>();
        void Row(string name, Func<SimulationResult, double> f)
        {
            var s = Statistics.Describe(r.Select(f).ToArray());
            rows.Add(string.Join(';', name, F(s.Mean, 4), F(s.Std, 4), F(s.HalfWidth, 4), F(s.Min, 4), F(s.Max, 4)));
            Log($"{name,-6} {F(s.Mean)} ± {F(s.HalfWidth)} σ={F(s.Std)} [{F(s.Min)}; {F(s.Max)}]");
        }
        Row("T", x => x.MeanTime);
        Row("rk", x => x.RhoCheck);
        Row("rn", x => x.RhoAdjust);
        Row("L1", x => x.QueueCheck);
        Row("L2", x => x.QueueAdjust);
        Row("W1", x => x.WaitCheck);
        Row("W2", x => x.WaitAdjust);
        Row("N", x => x.MeanInSystem);
        Row("thr", x => x.Throughput);
        Row("chk", x => x.ChecksPerTv);
        Csv("baseline_stats.csv", "name;mean;std;half;min;max", rows);

        var chiRows = new List<string>();
        foreach (var (name, f) in new (string, Func<SimulationResult, double>)[]
                 { ("T", x => x.MeanTime), ("lnT", x => Math.Log(x.MeanTime)), ("rk", x => x.RhoCheck), ("rn", x => x.RhoAdjust) })
        {
            var data = r.Select(f).ToArray();
            var c = Statistics.ChiSquareNormal(data, 10);
            chiRows.Add(string.Join(';', name, F(c.Statistic, 3), c.DegreesOfFreedom, F(c.Critical, 3), F(c.PValue, 3), c.Accepted));
            Log($"χ² {name}: {F(c.Statistic, 3)} vs {F(c.Critical, 3)} (df={c.DegreesOfFreedom}) -> {(c.Accepted ? "прийнято" : "відхилено")}");
            Csv($"chi2_{name}.csv", "lo;hi;observed;expected",
                Enumerable.Range(0, c.Observed.Length).Select(i =>
                    $"{F(c.Edges[i], 5)};{F(c.Edges[i + 1], 5)};{c.Observed[i]};{F(c.Expected[i], 3)}"));
            var (he, hc) = Statistics.Histogram(data, 15);
            Csv($"hist_{name}.csv", "lo;hi;count", Enumerable.Range(0, hc.Length).Select(i => $"{F(he[i], 5)};{F(he[i + 1], 5)};{hc[i]}"));
        }
        Csv("chi2.csv", "name;chi2;df;critical;pvalue;accepted", chiRows);
        return r;
    }

    public void Regression(double[] levels)
    {
        Log("== Регресійний аналіз ==");
        var rows = new List<string>();
        var xs = new List<double[]>();
        var ys = new List<double>();
        var means = new List<(double A, double Rho, double T)>();
        foreach (double a in levels)
        {
            var p = ModelParameters.Base with { ArrivalMin = a - 2, ArrivalMax = a + 2 };
            var r = Series(p, Replications, seed0: 2000);
            double rho = p.RhoCheck;
            foreach (var x in r)
            {
                xs.Add([1, rho / (1 - rho)]);
                ys.Add(x.MeanTime);
            }
            var s = Statistics.Describe(r.Select(x => x.MeanTime).ToArray());
            means.Add((a, rho, s.Mean));
            rows.Add(string.Join(';', F(a, 2), F(rho), F(rho / (1 - rho), 4), F(s.Mean, 3), F(s.Std, 3), F(s.HalfWidth, 3),
                F(r.Average(x => x.RhoCheck)), F(r.Average(x => x.RhoAdjust))));
            Log($"a={F(a, 2)} ρ={F(rho)} T̄={F(s.Mean, 2)} ± {F(s.HalfWidth, 2)}");
        }
        var (b, R) = Statistics.LeastSquares(xs, ys);
        Log($"T = {F(b[0], 3)} + {F(b[1], 3)}·ρ/(1-ρ), R = {F(R)}");
        var (bl, Rl) = Statistics.LeastSquares(xs.Select((x, i) => new[] { 1, levels[i / Replications] }).ToList(), ys);
        Log($"Лінійна: T = {F(bl[0], 3)} + {F(bl[1], 3)}·a, R = {F(Rl)}");
        Csv("regression_levels.csv", "a;rho;x;T;std;half;rk;rn", rows);
        Csv("regression_coef.csv", "model;b0;b1;R",
            [$"hyper;{F(b[0], 4)};{F(b[1], 4)};{F(R)}", $"linear;{F(bl[0], 4)};{F(bl[1], 4)};{F(Rl)}"]);
    }

    public void Modifications()
    {
        Log("== Модифікації ==");
        var variants = new (string Code, string Name, ModelParameters P)[]
        {
            ("V0", "Базова модель", ModelParameters.Base),
            ("V1", "Надходження по 2 ТВ, U(7,15)", ModelParameters.Base with { Batch = 2, ArrivalMin = 7, ArrivalMax = 15 }),
            ("V2", "40% після налаштування на упакування", ModelParameters.Base with { PAdjustToPack = 0.4 }),
            ("V3", "Додатковий етап контролю", ModelParameters.Base with { PreCheck = true }),
            ("V23", "Модифікації 2 і 3 разом", ModelParameters.Base with { PAdjustToPack = 0.4, PreCheck = true }),
        };
        var rows = new List<string>();
        var groups = new List<IReadOnlyList<double>>();
        foreach (var (code, name, p) in variants)
        {
            var r = Series(p, Replications, seed0: 3000);
            var s = Statistics.Describe(r.Select(x => x.MeanTime).ToArray());
            groups.Add(r.Select(x => x.MeanTime).ToArray());
            rows.Add(string.Join(';', code, name, F(p.RhoCheck), F(r.Average(x => x.RhoCheck)), F(p.RhoAdjust),
                F(r.Average(x => x.RhoAdjust)), F(p.RhoPre), F(r.Average(x => x.RhoPre)), F(s.Mean, 3), F(s.HalfWidth, 3),
                F(r.Average(x => x.QueueCheck), 3), F(r.Average(x => x.QueueAdjust), 3), F(r.Average(x => x.QueuePre), 3),
                F(r.Average(x => x.Throughput), 3)));
            Log($"{code,-4} {name,-38} ρк {F(r.Average(x => x.RhoCheck))} ρн {F(r.Average(x => x.RhoAdjust))} ρ0 {F(r.Average(x => x.RhoPre))} T̄ {F(s.Mean, 2)} ± {F(s.HalfWidth, 2)}");
        }
        Csv("modifications.csv", "code;name;rk_t;rk;rn_t;rn;r0_t;r0;T;half;L1;L2;L0;thr", rows);

        var unstable = ModelParameters.Base with { Batch = 2 };
        var net = new TvLineNetwork(unstable, 1);
        var growth = new List<string>();
        for (double t = 1000; t <= 20000; t += 1000)
        {
            net.Model.Simulate(t);
            var c = net.Collect();
            growth.Add(string.Join(';', F(t, 0), net.Check.Queue, net.Adjust.Queue, F(c.RhoCheck), F(c.RhoAdjust)));
        }
        Csv("mod1_unstable.csv", "t;L1;L2;rk;rn", growth);
        Log($"V1 (U(3,5;7,5), по 2 ТВ): теор. ρк={F(unstable.RhoCheck)}; черга в СМО1 на t=20000: {net.Check.Queue}");

        var an = Statistics.Anova(groups.Take(4).ToList());
        Log($"ANOVA (V0..V3): Dф={F(an.FactorVariance, 2)} Dзал={F(an.ResidualVariance, 2)} F={F(an.F, 2)} Fкр={F(an.Critical, 3)} -> {(an.Significant ? "вплив значущий" : "вплив незначущий")}");
        var an2 = Statistics.Anova([groups[2], groups[3], groups[4]]);
        Log($"ANOVA (V2, V3, V23): F={F(an2.F, 2)} Fкр={F(an2.Critical, 3)} -> {(an2.Significant ? "значущий" : "незначущий")}");
        Csv("anova.csv", "set;Df;Dr;F;df1;df2;Fcr;significant",
        [
            $"V0-V3;{F(an.FactorVariance, 3)};{F(an.ResidualVariance, 3)};{F(an.F, 3)};{an.Df1};{an.Df2};{F(an.Critical, 3)};{an.Significant}",
            $"V2-V23;{F(an2.FactorVariance, 3)};{F(an2.ResidualVariance, 3)};{F(an2.F, 3)};{an2.Df1};{an2.Df2};{F(an2.Critical, 3)};{an2.Significant}",
        ]);
        Csv("mod_runs.csv", "run;" + string.Join(';', variants.Select(v => v.Code)),
            Enumerable.Range(0, Replications).Select(i => (i + 1) + ";" + string.Join(';', groups.Select(g => F(g[i], 3)))));
    }

    public void Proposals()
    {
        Log("== Пропозиції ==");
        var variants = new (string Name, ModelParameters P)[]
        {
            ("Базова модель", ModelParameters.Base),
            ("3 контролери", ModelParameters.Base with { Controllers = 3 }),
            ("2 настроювачі", ModelParameters.Base with { Adjusters = 2 }),
            ("3 контролери, 2 настроювачі", ModelParameters.Base with { Controllers = 3, Adjusters = 2 }),
            ("Модифікації 2 і 3", ModelParameters.Base with { PAdjustToPack = 0.4, PreCheck = true }),
        };
        var rows = new List<string>();
        foreach (var (name, p) in variants)
        {
            var r = Series(p, Replications, seed0: 4000);
            var s = Statistics.Describe(r.Select(x => x.MeanTime).ToArray());
            rows.Add(string.Join(';', name, F(r.Average(x => x.RhoCheck)), F(r.Average(x => x.RhoAdjust)), F(s.Mean, 3), F(s.HalfWidth, 3),
                F(r.Average(x => x.WaitCheck), 3), F(r.Average(x => x.WaitAdjust), 3)));
            Log($"{name,-30} ρк {F(r.Average(x => x.RhoCheck))} ρн {F(r.Average(x => x.RhoAdjust))} T̄ {F(s.Mean, 2)} ± {F(s.HalfWidth, 2)}");
        }
        Csv("proposals.csv", "name;rk;rn;T;half;W1;W2", rows);
    }

    public void SaveReport() => File.WriteAllText(Path.Combine(outDir, "report.txt"), report.ToString());
}
