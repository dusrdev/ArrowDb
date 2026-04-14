```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4.1 (25E253) [Darwin 25.4.0]
Apple M2 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method           | Count   | Mean         | Error        | StdDev       | Rank | Allocated   |
|----------------- |-------- |-------------:|-------------:|-------------:|-----:|------------:|
| **RandomOperations** | **100**     |     **38.94 μs** |     **3.074 μs** |     **4.409 μs** |    **1** |    **15.45 KB** |
| **RandomOperations** | **10000**   |  **1,354.82 μs** |    **89.848 μs** |   **131.698 μs** |    **2** |   **691.92 KB** |
| **RandomOperations** | **1000000** | **99,480.14 μs** | **2,425.217 μs** | **3,629.951 μs** |    **3** | **53652.41 KB** |
