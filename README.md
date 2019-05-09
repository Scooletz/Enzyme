![Icon](https://raw.githubusercontent.com/Scooletz/Enzyme/develop/package_icon.png)

*This repository is currently not maintained. It's public only for educational purposes.*

# Enzyme

Enzyme is an experimental .NET asymmetric serializer, designed for write-heavy enviroments with a synchronous (no `async`/`await`) flow. It's not a replacement for a serializer. You may think about it as a tool for writing down lots of telemetry or other event-based data.

## Design Principles

Below you can find key design principles that laid foundations for Enzyme.

### Asymmetric serialization

Enzyme tries to serialize data as efficiently as possible, providing no standard way to deserialize them. The only way to deserialize data is to use the visitor pattern, which enables to walk through the complex objects without deserializing the payload as a whole. This, combined with batching enables to batch writes and then process large chunks of data without deserializing whole payload.

### Use stack allocated memory

Enzyme uses the stack allocated memory. In C#/.NET you can easily obtain a chunk on a continuous stack memory by using `stackalloc byte[N]`. Since C# 7.2, this memory can be even assigned to `Span<byte>` in a safe context (no need for `unsafe`). To make this work, code needs to follow two assumptions:

1. the amount of memory that will be allocated needs to be know upfront
1. the memory cannot be returned as it's stackallocated and it can be only passed down

The first assumption can be made by estimating the amount of memory that will be needed for an object to be serialized. The esimation might:

1. be a constant, for objects with fields of fixed size (`bool`),
1. have an upper-boundar, for object with fields either of fixed size or limited size (for example `int` serialization size is max 5 bytes)
1. need to go through the object to calculate the maximum amount of memory in runtime (for example an array `[]`)

The serializer tries to apply the best effort to make the estimation constant, but it will move it to less efficient category if needed (constant->bounded->varsize). Even in the last category, some trickery is applied to make it less costly. For instance, to estimate an amout of memory for `int[]`, the estimator needs only to multiply the length of the array by 5 (5 - the maximum amount of bytes needed to write an `int` ). There's no need to go through the every item in an array. Measurements have shown, that even with visiting some members, this cost is amortized by much faster serialization.

The second assumption about not returning a stackallocated memory can be followed, by accepting a delegate or an interface that will accept `Span<byte>`. In the case of Enzyme, it's `IWriter` show without comments below:

```c#
public interface IWriter<TContext>
{
    void Write(ref TContext context, Span<byte> payload);
}
```

The `context` can be used to capture any state for the current serialization (like a struct with the final target where the data should be written to), without a need of allocating the writer over and over again.

If, because of the usage scenario, the payload should be written to an async API, the option for it would be to provide another method for the writer or even another writer, that would estimate the size, get `Memory<byte>` from a pool, write to it and then pass to the async API. This path has not been neither deeply considered nor implemented.

### Inlined methods with simple variables

After several testing different approaches, the fastest way that was found to serialize data, was to inline all the methods and use simple variables with no ref structs for context etc. Inlining means that some methods are a bit bigger (especially for types using other types as fields). At the same time, inlining enabled passing almost no parameters around and using (and re-using) variables defined in the top method (as there's only one).

A remark: some variables are re-used in an inappropriate way (re-using an `object` variable for anything that is not ref-like or a struct or a pointer), but this could be done nowadays with `Unsafe.As`.

### Benchmarking from the beginning

The whole work wouldn't be possible without [BenchmarkDotNet](https://benchmarkdotnet.org/). Probably the majority of the improvements was result of some findings in benchmarks. I cannot imagine writing a high performing code without this library anymore. It's like tests but for performance.

## Protocol

Below, there's a short description of a protocol used by Enzyme:

1. If a field has a null value, it is not written to the output payload.
1. If a field has a value, the value will be written with a 2-byte prefix called manifest.
1. The manifest includes both type of the fields and the field number.
1. If the value is of a complex type, then after writing the manifest of an object, the manifest of the first field will appear.
1. Types of variable and bounded length (strings, arrays) are also prefixed with their length.

## Things to address

There are several things that could be potentially addressed:

1. Endianess - more tests needed, probably usage of `BinaryPrimitives`.
1. Removal of the unsafe. With the new era of Span and various optimizations like a [propagated bound checks removal](https://github.com/dotnet/coreclr/pull/11521) etc., maybe there's no need for using unsafe.
1. Make Enzyme symmetric - maybe Enzyme could deserialize items without performance penalties and become a general serializer?

## Icon

[Molecule](https://thenounproject.com/term/molecule/1224075) designed by [Anton ICON](https://thenounproject.com/suhartonosmkn1panji/) from The Noun Project