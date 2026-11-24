using System.Diagnostics;
using TowerDominion.Toolkit.Rng;

const int warmup = 1_000_000;
const int iterations = 20_000_000;
var key = new RngKey("Run", "Combat", "TargetSelection");
var deriver = new Fnv1aKeyDeriver();

foreach (IRandomGeneratorFactory factory in new IRandomGeneratorFactory[]
{
    new Pcg32Factory(),
    new Xoshiro256StarStarFactory()
})
{
    IRandomGenerator raw = factory.Create(deriver.Derive(12345, key));
    for (int i = 0; i < warmup; i++) raw.NextUInt64();
    Measure(factory.AlgorithmId + " raw", iterations, () => raw.NextUInt64());

    var manager = new RngManager(12345, deriver, factory);
    for (int i = 0; i < warmup; i++) manager.NextUInt64(key);
    Measure(factory.AlgorithmId + " keyed", iterations, () => manager.NextUInt64(key));

    var handledManager = new RngManager(12345, deriver, factory);
    RngStream handle = handledManager.Resolve(key);
    for (int i = 0; i < warmup; i++) handle.NextUInt64();
    Measure(factory.AlgorithmId + " handle", iterations, () => handle.NextUInt64());
    Measure(factory.AlgorithmId + " handle-range", iterations, () => (ulong)handle.NextInt32(-2, 3));

    const int materializations = 100_000;
    ulong checksum = 0;
    var materializeManager = new RngManager(12345, deriver, factory);
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < materializations; i++)
        checksum ^= materializeManager.Resolve(new RngKey("Run", "Synthetic", "Stream-" + i)).NextUInt64();
    sw.Stop();
    Print(factory.AlgorithmId + " materialize", materializations, sw.Elapsed, checksum);

    const int activeStreams = 1024;
    const int manyDraws = 10_000_000;
    var manyManager = new RngManager(12345, deriver, factory);
    RngStream[] streams = Enumerable.Range(0, activeStreams)
        .Select(i => manyManager.Resolve(new RngKey("Run", "Synthetic", "Hot-" + i)))
        .ToArray();
    checksum = 0;
    sw.Restart();
    for (int i = 0; i < manyDraws; i++)
        checksum ^= streams[i & (activeStreams - 1)].NextUInt64();
    sw.Stop();
    Print(factory.AlgorithmId + " 1024 handles", manyDraws, sw.Elapsed, checksum);

    string json = RngSnapshotJson.Serialize(manyManager.CaptureSnapshot());
    const int snapshotIterations = 1000;
    sw.Restart();
    for (int i = 0; i < snapshotIterations; i++)
        checksum ^= (ulong)RngSnapshotJson.Serialize(manyManager.CaptureSnapshot()).Length;
    sw.Stop();
    Print(factory.AlgorithmId + " snapshot 1024", snapshotIterations, sw.Elapsed, checksum);
    Console.WriteLine($"{factory.AlgorithmId,-24} snapshot JSON {json.Length,8} bytes");
}

static void Measure(string name, int count, Func<ulong> operation)
{
    ulong checksum = 0;
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < count; i++) checksum ^= operation();
    sw.Stop();
    Print(name, count, sw.Elapsed, checksum);
}

static void Print(string name, int count, TimeSpan elapsed, ulong checksum)
{
    double ns = elapsed.TotalMilliseconds * 1_000_000 / count;
    double millionPerSecond = count / elapsed.TotalSeconds / 1_000_000;
    Console.WriteLine($"{name,-28} {ns,10:F2} ns/op  {millionPerSecond,8:F2} Mops/s  checksum={checksum:X16}");
}
