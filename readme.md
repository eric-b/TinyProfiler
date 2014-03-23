# TinyProfiler

## What is it ?

TinyProfiler is a simple profiling solution, based on the `using` keyword (it implements `IDisposable`).  
It offers an effective and fast way to measure execution time by regions of code.

You can copy the source into your project or install the [nuget package](http://nuget.org/packages/TinyProfiler/) (embeds the source code into your project, no assembly reference required).

## Other solutions

If your project is web based, I highly suggest you to go with [MiniProfiler](http://miniprofiler.com/).  
TinyProfiler is a poor man's profiling solution easy to embed in any .NET 3.5 project without third party assembly reference. I typically put this code in my logging library, and I wire the actual implementation via the application IoC container.

## Usage

TinyProfiler is basically a contract based on two interfaces:

- `IProfilerFactory`
- `IProfiler : IDisposable`

There is an implementation based on a `ILogger` interface (where to write the measures) but you can easily adapt the code to your needs. There is no wiki but the code is simple and commented. You can also read the unit tests for more samples.

	using (var profiler = profilerFactory.StartProfiling("my operation"))
	{
		using (var step1 = profiler.StartStep("Step 1"))
		{
			// Lengthy operation...
		}
		
		using (var step2 = profiler.StartStep("Step 2"))
		{
			// Lengthy operation...
		}
	}

Output sample:

	my operation: 453 ms
	  - Step 1: 41 ms
	  - Step 2: 412 ms (+41)
	  
## Limitations, caveats, known bugs

[Let me know](https://github.com/eric-b/TinyProfiler/issues) if you have troubles with use of this code.
