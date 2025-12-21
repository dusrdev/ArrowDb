```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.1 (25B78) [Darwin 25.1.0]
Apple M2 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.101
  [Host]    : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  MediumRun : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method           | Count   | Mean         | Error        | StdDev       | Rank | Allocated   |
|----------------- |-------- |-------------:|-------------:|-------------:|-----:|------------:|
| **RandomOperations** | **100**     |     **41.73 μs** |     **3.662 μs** |     **5.252 μs** |    **1** |    **15.84 KB** |
| **RandomOperations** | **10000**   |  **1,349.40 μs** |    **65.665 μs** |    **89.883 μs** |    **2** |   **701.72 KB** |
| **RandomOperations** | **1000000** | **98,975.55 μs** | **1,918.205 μs** | **2,811.681 μs** |    **3** | **53612.05 KB** |
