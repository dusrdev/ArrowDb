```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.1 (25B78) [Darwin 25.1.0]
Apple M2 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.101
  [Host]    : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  MediumRun : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method         | Size    | Mean         | Error       | StdDev      | Rank | Allocated |
|--------------- |-------- |-------------:|------------:|------------:|-----:|----------:|
| **SerializeAsync** | **100**     |     **207.4 μs** |    **17.56 μs** |    **25.73 μs** |    **1** |     **520 B** |
| **SerializeAsync** | **10000**   |   **2,409.7 μs** |   **333.98 μs** |   **499.89 μs** |    **2** |     **520 B** |
| **SerializeAsync** | **1000000** | **144,343.7 μs** | **1,514.16 μs** | **2,219.44 μs** |    **3** |     **520 B** |
