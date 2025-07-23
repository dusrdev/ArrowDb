```

BenchmarkDotNet v0.15.2, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M2 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 9.0.303
  [Host]    : .NET 9.0.7 (9.0.725.31616), Arm64 RyuJIT AdvSIMD
  MediumRun : .NET 9.0.7 (9.0.725.31616), Arm64 RyuJIT AdvSIMD

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method           | Count   | Mean          | Error        | StdDev       | Rank | Allocated   |
|----------------- |-------- |--------------:|-------------:|-------------:|-----:|------------:|
| **RandomOperations** | **100**     |      **41.75 μs** |     **5.523 μs** |     **8.096 μs** |    **1** |    **15.11 KB** |
| **RandomOperations** | **10000**   |   **1,779.51 μs** |   **484.766 μs** |   **679.575 μs** |    **2** |   **722.35 KB** |
| **RandomOperations** | **1000000** | **101,016.96 μs** | **1,921.740 μs** | **2,876.370 μs** |    **3** | **53529.98 KB** |
