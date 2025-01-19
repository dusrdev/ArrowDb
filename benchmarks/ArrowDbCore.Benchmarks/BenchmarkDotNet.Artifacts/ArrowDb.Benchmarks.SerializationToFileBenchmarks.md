# ArrowDb.Benchmarks.SerializationToFileBenchmarks

```log
BenchmarkDotNet v0.14.0, macOS Sequoia 15.2 (24C101) [Darwin 24.2.0]
Apple M2 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 9.0.100
  [Host]    : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD
  MediumRun : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD

Job=MediumRun  InvocationCount=1  IterationCount=15
LaunchCount=2  UnrollFactor=1  WarmupCount=10
```

| Method         | Size    | Mean         | Error       | StdDev      | Rank | Allocated |
|--------------- |-------- |-------------:|------------:|------------:|-----:|----------:|
| **SerializeAsync** | **100**     |     **117.5 μs** |     **9.29 μs** |    **13.33 μs** |    **1** |   **2.45 KB** |
| **SerializeAsync** | **10000**   |   **1,671.1 μs** |   **151.09 μs** |   **221.46 μs** |    **2** |   **2.63 KB** |
| **SerializeAsync** | **1000000** | **158,980.0 μs** | **1,682.80 μs** | **2,413.43 μs** |    **3** |   **2.02 KB** |
