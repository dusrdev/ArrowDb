# ArrowDb.Benchmarks.RandomOperationsBenchmarks

```log
BenchmarkDotNet v0.14.0, macOS Sequoia 15.2 (24C101) [Darwin 24.2.0]
Apple M2 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 9.0.100
  [Host]    : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD
  MediumRun : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD

Job=MediumRun  InvocationCount=1  IterationCount=15
LaunchCount=2  UnrollFactor=1  WarmupCount=10
```

| Method           | Count   | Mean          | Error        | StdDev       | Rank | Allocated   |
|----------------- |-------- |--------------:|-------------:|-------------:|-----:|------------:|
| **RandomOperations** | **100**     |      **39.41 μs** |     **2.790 μs** |     **4.090 μs** |    **1** |    **14.56 KB** |
| **RandomOperations** | **10000**   |   **1,724.96 μs** |   **487.760 μs** |   **699.531 μs** |    **2** |  **1008.37 KB** |
| **RandomOperations** | **1000000** | **104,545.08 μs** | **1,799.174 μs** | **2,637.206 μs** |    **3** | **62710.32 KB** |
