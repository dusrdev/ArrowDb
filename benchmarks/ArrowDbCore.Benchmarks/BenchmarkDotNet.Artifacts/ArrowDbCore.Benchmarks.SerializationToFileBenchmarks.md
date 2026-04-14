```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4.1 (25E253) [Darwin 25.4.0]
Apple M2 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a

Job=MediumRun  InvocationCount=1  IterationCount=15
LaunchCount=2  UnrollFactor=1  WarmupCount=10

```
| Method         | Size    | Mean         | Error       | StdDev      | Rank | Allocated |
|--------------- |-------- |-------------:|------------:|------------:|-----:|----------:|
| **SerializeAsync** | **100**     |     **214.0 μs** |    **20.65 μs** |    **30.90 μs** |    **1** |   **2.22 KB** |
| **SerializeAsync** | **10000**   |   **2,423.6 μs** |   **141.85 μs** |   **203.43 μs** |    **2** |   **9.48 KB** |
| **SerializeAsync** | **1000000** | **160,746.6 μs** | **2,265.35 μs** | **3,320.52 μs** |    **3** | **760.73 KB** |
