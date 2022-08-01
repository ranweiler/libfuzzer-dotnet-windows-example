# Fuzzing C# on Windows with SharpFuzz and `libfuzzer-dotnet`

## Overview

[SharpFuzz](https://github.com/metalnem/sharpfuzz) provides two key capabilities:
- A CLI tool that rewrites assemblies to enable _coverage feedback_
- C# APIs for authoring _fuzzing harnesses_

SharpFuzz was originally written to support fuzzing via [AFL](https://github.com/google/AFL).
The [`libfuzzer-dotnet`](https://github.com/metalnem/libfuzzer-dotnet) sibling project
enables using a SharpFuzz harness with LibFuzzer as the _fuzzing engine_,
on either Linux or Windows.

> **Note:** From a LibFuzzer perspective, `libfuzzer-dotnet` is an atypical repurposing of the LibFuzzer engine and APIs.
> A normal LibFuzzer binary links in both some fixed, user-provided code under test
> _and_ a compiler-provided fuzzing engine and runtime.
> The result is one LibFuzzer binary per fuzzing target.
> In contrast, `libfuzzer-dotnet` is a _single_ LibFuzzer that has a special `--target_path` parameter.
> This is used to specify the path to a self-contained fuzzing harness,
> which is then spawned and managed with parent-child IPC.
> In this way, `libfuzzer-dotnet` uses LibFuzzer to create a special stand-alone fuzzing executor
> for CLR code.

## Requirements

- Clang >= 14.0.0 (for now)
- .NET 6.0 SDK
- `SharpFuzz` library >= 2.0
- `sharpfuzz` CLI tool >= 1.0

## Steps

## Install the SharpFuzz CLI tool

Using the `dotnet` CLI, invoke:

```powershell
dotnet tool install --global SharpFuzz.CommandLine
```

### Build `libfuzzer-dotnet`

Download the source for the Windows version of `libfuzzer-dotnet`.

```powershell
# Snapshot that matches release of SharpFuzz 2.0.
$src = "https://raw.githubusercontent.com/Metalnem/libfuzzer-dotnet/55d84f84b3540c864371e855c2a5ecb728865d97/libfuzzer-dotnet-windows.cc"

iwr $src -o libfuzzer-dotnet-windows.cc
```

Compile the (parameterized) fuzzer.

```powershell
clang -g -O2 -fsanitize=fuzzer .\libfuzzer-dotnet-windows.cc -o libfuzzer-dotnet.exe
```

You should now have an executable named `libfuzzer-dotnet.exe`.
This is a special, _parameterized_ LibFuzzer which
_requires_ a non-standard `--target_path` argument.
A `libfuzzer-dotnet` binary only needs to be created once per native platform.
It does _not_ need to be recompiled for each CLR target.

In the next step, we will create this fuzzing target by
[publishing](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)
a [self-contained](https://docs.microsoft.com/en-us/dotnet/core/deploying/deploy-with-cli#self-contained-deployment)
native executable (and supporting directory) that links our _fuzzing target_.

### Build the self-contained target

Our example code under test can be found in the `Example` project.
The accompanying fuzzing harness can be found in the `ExampleFuzzer` project.
Note, these do not _need_ to be distinct projects.
In this case, they are, and `ExampleFuzzer` depends on the `Example` project.

We need to build and publish `ExampleFuzzer` as a self-contained executable deployment.
This will provide a platform-specific executable that bundles a .NET runtime.
After that, we will instrument the DLL for our actual target code.

From the repo root, invoke:

```powershell
mkdir out
dotnet publish ExampleFuzzer -c Release -o out --sc -r win10-x64
```

Now instrument the DLL that contains our fuzzing target code.
This lets the LibFuzzer engine detect when randomly-generated test cases uncover new code,
and significantly improves fuzzing efficacy.

```
sharpfuzz out/Example.dll
```

### Run the fuzzer

Now, you should be able to run the fuzzer like so:

```powershell
# Create a directory to save inputs that increased code coverage.
mkdir corpus

# Run the parameterized fuzzer with our self-contained target.
./libfuzzer-dotnet --target_path=out/ExampleFuzzer.exe corpus
```

You can reproduce discovered crashes with the LibFuzzer harness:

```powershell
./libfuzzer-dotnet.exe --target_path=out/ExampleFuzzer.exe ./crash.txt
```

Or without, using the self-contained assembly directly:

```powershell
./out/ExampleFuzzer.exe crash.txt
```
