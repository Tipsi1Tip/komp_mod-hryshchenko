namespace TvLine.Experiments;

public sealed record Summary(int N, double Mean, double Std, double Min, double Max, double HalfWidth);

public sealed record ChiSquareResult(double Statistic, int DegreesOfFreedom, double Critical, double PValue,
    double[] Edges, int[] Observed, double[] Expected)
{
    public bool Accepted => Statistic < Critical;
}

public sealed record AnovaResult(double FactorVariance, double ResidualVariance, double F, int Df1, int Df2, double Critical)
{
    public bool Significant => F > Critical;
}

public static class Statistics
{
    public static Summary Describe(IReadOnlyList<double> x, double beta = 0.95)
    {
        int n = x.Count;
        double mean = x.Average();
        double std = n > 1 ? Math.Sqrt(x.Sum(v => (v - mean) * (v - mean)) / (n - 1)) : 0;
        double t = n > 1 ? StudentQuantile(1 - (1 - beta) / 2, n - 1) : 0;
        return new Summary(n, mean, std, x.Min(), x.Max(), t * std / Math.Sqrt(n));
    }

    public static int Replications(double sigma, double eps, double beta = 0.95) =>
        (int)Math.Ceiling(sigma * sigma / (eps * eps * (1 - beta)));

    public static ChiSquareResult ChiSquareNormal(IReadOnlyList<double> x, int k = 10, double alpha = 0.05)
    {
        var s = Describe(x);
        var edges = new double[k + 1];
        edges[0] = double.NegativeInfinity;
        edges[k] = double.PositiveInfinity;
        for (int i = 1; i < k; i++) edges[i] = s.Mean + s.Std * NormalQuantile((double)i / k);
        return ChiSquare(x, edges, 2, v => NormalCdf((v - s.Mean) / s.Std), alpha);
    }

    public static ChiSquareResult ChiSquare(IReadOnlyList<double> x, double[] edges, int estimated,
        Func<double, double> cdf, double alpha = 0.05)
    {
        int k = edges.Length - 1, n = x.Count;
        var observed = new int[k];
        foreach (double v in x)
        {
            int i = 0;
            while (i < k - 1 && v >= edges[i + 1]) i++;
            observed[i]++;
        }
        var expected = new double[k];
        for (int i = 0; i < k; i++)
        {
            double lo = double.IsNegativeInfinity(edges[i]) ? 0 : cdf(edges[i]);
            double hi = double.IsPositiveInfinity(edges[i + 1]) ? 1 : cdf(edges[i + 1]);
            expected[i] = n * (hi - lo);
        }
        double chi = 0;
        for (int i = 0; i < k; i++) chi += (observed[i] - expected[i]) * (observed[i] - expected[i]) / expected[i];
        int df = k - 1 - estimated;
        return new ChiSquareResult(chi, df, ChiSquareQuantile(1 - alpha, df), 1 - ChiSquareCdf(chi, df),
            edges, observed, expected);
    }

    public static (double[] Edges, int[] Counts) Histogram(IReadOnlyList<double> x, int k)
    {
        double min = x.Min(), h = (x.Max() - min) / k;
        var edges = Enumerable.Range(0, k + 1).Select(i => min + i * h).ToArray();
        var counts = new int[k];
        foreach (double v in x) counts[Math.Min(k - 1, (int)((v - min) / h))]++;
        return (edges, counts);
    }

    public static double NormalQuantile(double p) => Invert(NormalCdf, p, -10, 10);

    public static (double[] B, double R) LeastSquares(IReadOnlyList<double[]> rows, IReadOnlyList<double> y)
    {
        int m = rows[0].Length;
        var a = new double[m, m + 1];
        for (int j = 0; j < rows.Count; j++)
        for (int r = 0; r < m; r++)
        {
            for (int c = 0; c < m; c++) a[r, c] += rows[j][r] * rows[j][c];
            a[r, m] += rows[j][r] * y[j];
        }
        for (int col = 0; col < m; col++)
        {
            int piv = col;
            for (int r = col + 1; r < m; r++) if (Math.Abs(a[r, col]) > Math.Abs(a[piv, col])) piv = r;
            for (int c = 0; c <= m; c++) (a[col, c], a[piv, c]) = (a[piv, c], a[col, c]);
            for (int r = 0; r < m; r++)
            {
                if (r == col) continue;
                double f = a[r, col] / a[col, col];
                for (int c = col; c <= m; c++) a[r, c] -= f * a[col, c];
            }
        }
        var b = new double[m];
        for (int r = 0; r < m; r++) b[r] = a[r, m] / a[r, r];

        double mean = y.Average(), ssRes = 0, ssTot = 0;
        for (int j = 0; j < rows.Count; j++)
        {
            double yh = 0;
            for (int r = 0; r < m; r++) yh += b[r] * rows[j][r];
            ssRes += (y[j] - yh) * (y[j] - yh);
            ssTot += (y[j] - mean) * (y[j] - mean);
        }
        return (b, Math.Sqrt(Math.Max(0, 1 - ssRes / ssTot)));
    }

    public static AnovaResult Anova(IReadOnlyList<IReadOnlyList<double>> groups, double alpha = 0.05)
    {
        int q = groups.Count;
        int p = groups[0].Count;
        double total = groups.SelectMany(g => g).Average();
        double ssF = 0, ssR = 0;
        foreach (var g in groups)
        {
            double m = g.Average();
            ssF += p * (m - total) * (m - total);
            ssR += g.Sum(v => (v - m) * (v - m));
        }
        int df1 = q - 1, df2 = q * (p - 1);
        double dF = ssF / df1, dR = ssR / df2;
        return new AnovaResult(dF, dR, dF / dR, df1, df2, FisherQuantile(1 - alpha, df1, df2));
    }

    public static double NormalCdf(double z) => 0.5 * Erfc(-z / Math.Sqrt(2));

    private static double Erfc(double x)
    {
        double z = Math.Abs(x);
        double t = 1 / (1 + 0.5 * z);
        double r = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 + t * (0.37409196 + t * (0.09678418 +
                   t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 + t * (1.48851587 +
                   t * (-0.82215223 + t * 0.17087277)))))))));
        return x >= 0 ? r : 2 - r;
    }

    public static double ChiSquareCdf(double x, int k) => GammaP(k / 2.0, x / 2.0);

    public static double ChiSquareQuantile(double p, int k) => Invert(x => ChiSquareCdf(x, k), p, 0, 1000);

    public static double StudentCdf(double t, int df)
    {
        double x = df / (df + t * t);
        double tail = 0.5 * BetaI(df / 2.0, 0.5, x);
        return t >= 0 ? 1 - tail : tail;
    }

    public static double StudentQuantile(double p, int df) => Invert(t => StudentCdf(t, df), p, -100, 100);

    public static double FisherCdf(double f, int d1, int d2) =>
        f <= 0 ? 0 : BetaI(d1 / 2.0, d2 / 2.0, d1 * f / (d1 * f + d2));

    public static double FisherQuantile(double p, int d1, int d2) => Invert(f => FisherCdf(f, d1, d2), p, 0, 1000);

    private static double Invert(Func<double, double> cdf, double p, double lo, double hi)
    {
        for (int i = 0; i < 200; i++)
        {
            double mid = (lo + hi) / 2;
            if (cdf(mid) < p) lo = mid; else hi = mid;
        }
        return (lo + hi) / 2;
    }

    private static double GammaLn(double x)
    {
        double[] c = [76.18009172947146, -86.50532032941677, 24.01409824083091,
                      -1.231739572450155, 0.1208650973866179e-2, -0.5395239384953e-5];
        double y = x, tmp = x + 5.5;
        tmp -= (x + 0.5) * Math.Log(tmp);
        double ser = 1.000000000190015;
        foreach (double v in c) ser += v / ++y;
        return -tmp + Math.Log(2.5066282746310005 * ser / x);
    }

    private static double GammaP(double a, double x)
    {
        if (x <= 0) return 0;
        if (x < a + 1)
        {
            double ap = a, sum = 1 / a, del = sum;
            for (int n = 0; n < 500; n++)
            {
                del *= x / ++ap;
                sum += del;
                if (Math.Abs(del) < Math.Abs(sum) * 1e-14) break;
            }
            return sum * Math.Exp(-x + a * Math.Log(x) - GammaLn(a));
        }
        double b = x + 1 - a, cc = 1e300, d = 1 / b, h = d;
        for (int i = 1; i < 500; i++)
        {
            double an = -i * (i - a);
            b += 2;
            d = an * d + b; if (Math.Abs(d) < 1e-300) d = 1e-300;
            cc = b + an / cc; if (Math.Abs(cc) < 1e-300) cc = 1e-300;
            d = 1 / d;
            double del = d * cc;
            h *= del;
            if (Math.Abs(del - 1) < 1e-14) break;
        }
        return 1 - Math.Exp(-x + a * Math.Log(x) - GammaLn(a)) * h;
    }

    private static double BetaI(double a, double b, double x)
    {
        if (x <= 0) return 0;
        if (x >= 1) return 1;
        double bt = Math.Exp(GammaLn(a + b) - GammaLn(a) - GammaLn(b) + a * Math.Log(x) + b * Math.Log(1 - x));
        return x < (a + 1) / (a + b + 2) ? bt * BetaCf(a, b, x) / a : 1 - bt * BetaCf(b, a, 1 - x) / b;
    }

    private static double BetaCf(double a, double b, double x)
    {
        double qab = a + b, qap = a + 1, qam = a - 1, c = 1, d = 1 - qab * x / qap;
        if (Math.Abs(d) < 1e-300) d = 1e-300;
        d = 1 / d;
        double h = d;
        for (int m = 1; m <= 500; m++)
        {
            int m2 = 2 * m;
            double aa = m * (b - m) * x / ((qam + m2) * (a + m2));
            d = 1 + aa * d; if (Math.Abs(d) < 1e-300) d = 1e-300;
            c = 1 + aa / c; if (Math.Abs(c) < 1e-300) c = 1e-300;
            d = 1 / d; h *= d * c;
            aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
            d = 1 + aa * d; if (Math.Abs(d) < 1e-300) d = 1e-300;
            c = 1 + aa / c; if (Math.Abs(c) < 1e-300) c = 1e-300;
            d = 1 / d;
            double del = d * c;
            h *= del;
            if (Math.Abs(del - 1) < 1e-14) break;
        }
        return h;
    }
}
