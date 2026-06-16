# Large File Sorting - External merge sort


## Disclaimer

This repository is an experiment in using Codex to solve the classic [external sorting](https://en.wikipedia.org/wiki/External_sorting) problem. To avoid inventing a synthetic problem statement from scratch, it uses a public test assignment found online as the starting point for the exercise.  
During the work on this assignment, one senior engineer, several Codex agents, and a fair amount of Russian profanity were observed. No agents were harmed in the process.

## Assignment

Process a large text file where each line follows the format:

`<Number>. <String>`

### Example input

```
415. Apple
30432. Something something something
1. Apple
32. Cherry is the best
2. Banana is yellow
```

### Sorting criteria

1. Sort primarily by the string part (alphabetically).
2. If two lines have the same string, sort by the number (ascending).

### Expected output

```
1. Apple
415. Apple
2. Banana is yellow
32. Cherry is the best
30432. Something something something
```

## Implementation requirements

You must develop two C# programs:

1. Test File Generator
	- Creates a text file in the described format.
	- Ensures some lines share the same string part.
	- Allows specifying the target file size.

2. Sorter
	- Sorts the file according to the criteria above.
	- Must handle very large files (e.g. ~100 GB) efficiently.

## Evaluation criteria

Solutions will be assessed on:

- Code quality and readability
- Performance considerations. Benchmarking and performance testing will be considered a bonus.
- Memory management
- Test coverage to ensure reliability.
- Multithreading and concurrency


## Implementation Assumptions
### About the file and its format:
1. The file is a simple text file in UTF8 encoding.
2. One line of the file is one line to be sorted. Multi-line values are not allowed.
3. Duplicate lines are possible in the file.
4. Lines are not infinite and have a reasonable size from 3 to 1024 characters.
5. The range of allowed characters is standard ~~ASCII~~ UTF8.
6. The numeric part of the line: only positive numbers. The maximum expected range is Int64.

### About the Test File Generator:
1. Is it acceptable to specify the number of lines as an input value instead of the file size?

### About the Sorter:
1. Is it acceptable to output the sorted content as a separate file? Yes
2. Is it acceptable to create temporary files? Yes
3. If there are duplicates in the source file, is it acceptable to ignore them? Duplicate rows are allowed and must be taken into account by the algorithm. 
4. Sorting the string portion alphabetically ascending.

### Host machine:
1. Windows OS - Preferably, no OS tie-in is required. Windows OS is acceptable, provided it is properly optimized for it.
2. Available memory: 16 GB
3. Has free hard drive space for temporary files and a separately sorted file. There will be enough space, but the algorithm shouldn't rely on it. Proper error handling is expected.

## Repository Structure Design Decisions

- A single repository was used even though the assignment contains two separate applications. This keeps the full workflow in one place: file generation, file sorting, shared documentation, runner scripts, and performance notes can evolve together without being split across multiple repositories.
- Each application has its own solution file so their implementations stay operationally independent. `DummyFile.Generator` and `LargeFile.Sorter` can be built, tested, published, and evolved separately without forcing a shared solution structure where it is not needed.
- `LargeFiles/` stores generated input files and produced sorted output files for manual runs and large-scenario testing. Added to `.gitignore`
- `Runners/` stores PowerShell helper scripts for publishing and launching the generator and sorter in repeatable scenarios such as `100 MB`, `1 GB`, `10 GB`, and `100 GB`.
- `temp/` is the working directory for temporary chunk files and intermediate merge files created by the sorter during external sorting. Added to `.gitignore`

## Dummy File Generator

### Design Decisions

- The File Generator console application is intentionally kept as simple as possible. Because of that, it does not use the standard .NET application infrastructure such as Host Builder, Dependency Injection containers, or configuration binding from multiple sources.
- File system access is isolated behind dedicated adapter abstractions `IFileAdapter`. This keeps file creation and stream opening concerns separated from the higher-level export workflow `IFileExporter` and makes that logic easier to test in isolation.
- Data generation is separated from file export. This allows the generation pipeline and the disk writing pipeline to be verified independently and makes future changes safer and more localized.
- `IRowFormatter` was introduced intentionally as a separate abstraction so the output row format can evolve without coupling that change to the generator or exporter. For example, the separator could be changed from `". "` to `","` in the future with minimal impact on the rest of the application.

### Developed Features

- Generates output rows in the `<Number>. <String>` format.
- Supports generation limits by requested line count via `--file-size-lines`.
- Supports generation limits by requested file size via `--file-size` with `mb` and `gb` suffixes.
- Supports overwriting an existing output file via the `--force` flag.
- Validates command-line arguments and prints help information for invalid or incomplete input.
- Validates that enough free disk space is available before generation starts by checking the requested file size plus a 5% safety margin.
- Uses a dedicated file adapter layer to isolate file system operations from export logic.
- Uses a buffered file exporter that writes rows asynchronously in batches to reduce write overhead.
- Separates item generation, row formatting, and file export into isolated components to keep the implementation testable and easier to evolve.
- Includes periodic progress reporting with a progress bar-style log output for long-running generation.
- Supports graceful cancellation with `Ctrl+C`.
- Includes unit and integration-style tests for parsing, validation, generation, formatting, file handling, and export behavior.
- Includes a small performance measurement script for running the published generator and collecting execution time, output file size, and memory usage.

### Dummy File Generator TODO

- Add multithreaded generation, if it worth it
- Reduce per-row allocations in `ItemsGenerator` to improve generation throughput.

## Large File Sorter

### Design Decisions

- The sorter console application uses the standard .NET application infrastructure with `HostApplicationBuilder`, dependency injection, and structured logging. This was chosen because the sorting pipeline is materially more complex than the generator and benefits from explicit composition and lifecycle management.
- The sorter is built around the **external merge sort** algorithm with **k-way merge**. This approach was chosen because the assignment explicitly targets files that are much larger than available memory, so the solution must split the source into sorted chunks and then merge those chunks back into the final output.
- Input, chunk, and merge file access are isolated behind dedicated temporary file abstractions. This keeps file system stream ownership separate from sorting and merge orchestration, and makes disposal and cleanup rules explicit.
- The implementation follows an external sorting design. The source file is split into bounded sorted chunks first, and only then merged. This keeps the algorithm workable for very large inputs that cannot fit into memory.
- Chunk processing and merge processing intentionally reuse shared abstractions such as `ITempFileAdapter`, `ITempFileWriter`, and `ITempFileReader`. This reduces duplication between chunk files and intermediate merge files and keeps the temp-file lifecycle consistent across phases.
- Chunk records are represented as parsed `Item` objects that store the numeric part separately from the UTF-8 bytes of the text part. This avoids allocating a managed `string` per row and keeps in-memory sorting more memory-efficient.
- Formatting is isolated behind `IItemFormatter`. The default implementation writes text rows, while a binary formatter contract is already reserved for future optimization without forcing changes into the sorting pipeline.
- Chunk execution concurrency is intentionally gated by `IChunkExecutionLimiter`, which combines memory and CPU heuristics. This keeps the number of active chunk sorters bounded instead of letting the reader flood the machine with too many in-flight chunks.
- Chunking progress reporting is isolated behind a dedicated `IChunkingProgressReporter`. This keeps progress aggregation separate from the reader and sorter orchestration logic and allows periodic progress logs to stay centralized and thread-safe.
- Merge is implemented as a multi-pass batched k-way merge. Each pass merges at most `MergeConfig.MaxChunkFilesPerMerge` files into intermediate outputs until only one file remains. This avoids relying on opening an arbitrary number of files simultaneously.
- Merge batch execution is parallelized at the batch level through a dedicated coordinator and execution limiter. This allows independent merge batches from the same pass to run concurrently without parallelizing the inner `PriorityQueue` merge loop itself.
- The merge phase uses `PriorityQueue` as the in-memory structure for selecting the next smallest row across active temp files. This keeps the k-way merge logic simple, explicit, and efficient for batched merge passes.
- The default value of `MergeConfig.MaxChunkFilesPerMerge` was chosen empirically as `64`. With the default chunk size of `128 MB`, a `100 GB` source file is expected to produce about `800` chunk files, and that fan-in allows the merge phase to finish in two passes.

### Developed Features

- Sorts rows in the `<Number>. <String>` format by text ascending and number ascending for ties.
- Supports command-line parameters for input file `--file ..\LargeFiles\unsorted-1gb.txt`, output file `--output-file ..\LargeFiles\sorted-1gb.txt`, temp files directory `--temp-files-dir ..\temp`, overwrite mode `--force`, and help output `--help`. 
- Supports graceful cancellation with `Ctrl+C`.
- Uses chunk-based external sorting with a configurable 128 MB chunk target and newline-aware chunk boundaries.
- Supports bounded chunk processing with **concurrent execution limiting** based on configured chunk size, available memory, and detected level of parallelism.
- Reads the source file through `System.IO.Pipelines` and pushes chunk data into sorter pipelines without loading the entire input into memory.
- Parses rows into compact `Item` objects that store `long Number` and UTF-8 `TextBytes`.
- Sorts chunk items in memory and writes sorted chunk files through buffered temp-file writers.
- Uses explicit temp-file ownership so chunk and merge file streams are opened, completed, and disposed by their adapters.
- Supports single-file promotion when only one sorted chunk remains.
- Supports multi-pass merge batching using `MergeConfig.MaxChunkFilesPerMerge`, including cleanup of intermediate merge files between passes.
- Supports parallel batch merging within a merge pass while keeping each individual k-way merge batch sequential and deterministic.
- Uses pooled reusable formatting buffers during merge to reduce repeated temporary allocations.
- Includes a dedicated progress reporter for the chunking phase with periodic aggregated logging.
- Includes dedicated periodic progress reporting for both chunking and merge phases, including batch-level merge write progress.
- Logs environment information, chunk execution limits, and final memory metrics through the application logger.
- Includes unit and integration-style tests for command-line parsing, host wiring, input reading, chunk sorting, temp-file lifecycle, merge behavior, and end-to-end application flow.

### Large File Sorter TODO

- Add dynamic loading balancing
- Add binary sorting 

## Runners

Launch scripts live in the root `Runners/` folder.

The runner scripts were written in PowerShell because Windows was explicitly allowed by the assignment assumptions. At the same time, the overall application logic is not tied to Windows, and the published sorter and generator can also be executed from Linux with equivalent command-line arguments.

The sorter runner scripts also set an **explicit managed memory cap** through the `DOTNET_GCHeapHardLimit` environment variable.
After each runner finishes, it clears the environment variables it set for the current PowerShell process.

### Publish

- `file-generator-release.ps1` publishes `DummyFile.Generator` into `DummyFile.Generator/Release`
- `file-sorter-release.ps1` publishes `LargeFile.Sorter` into `LargeFile.Sorter/Release`

### Dummy File Generator

- `file-generator-run-100-lines.ps1` writes `LargeFiles/unsorted-100-lines.txt`
- `file-generator-run-100mb.ps1` writes `LargeFiles/unsorted-100mb.txt`
- `file-generator-run-1gb.ps1` writes `LargeFiles/unsorted-1gb.txt`
- `file-generator-run-10gb.ps1` writes `LargeFiles/unsorted-10gb.txt`
- `file-generator-run-100gb.ps1` writes `LargeFiles/unsorted-100gb.txt`

### Sorter

- `file-sorter-run-100-lines.ps1` reads `LargeFiles/unsorted-100-lines.txt` and writes `LargeFiles/sorted-100-lines.txt`
- `file-sorter-run-100mb.ps1` reads `LargeFiles/unsorted-100mb.txt` and writes `LargeFiles/sorted-100mb.txt`
- `file-sorter-run-1gb.ps1` reads `LargeFiles/unsorted-1gb.txt` and writes `LargeFiles/sorted-1gb.txt`
- `file-sorter-run-10gb.ps1` reads `LargeFiles/unsorted-10gb.txt` and writes `LargeFiles/sorted-10gb.txt`
- `file-sorter-run-100gb.ps1` reads `LargeFiles/unsorted-100gb.txt` and writes `LargeFiles/sorted-100gb.txt`

### Performance Testing

- `PerformanceTesting/file-sorter-run-10gb-with-counters.ps1` runs the released sorter executable.
- It shows the application logs and counter output in the console.
- It writes application stdout and stderr to `PerformanceTesting/logs/`.
- It writes counters output to `PerformanceTesting/file-sorter-run-10gb-metrics.log`.

### Example Scenario: 10 GB

1. Run `file-generator-release.ps1` to publish the generator into `DummyFile.Generator/Release`.
2. Run `file-generator-run-10gb.ps1` to create `LargeFiles/unsorted-10gb.txt`.
3. Run `file-sorter-release.ps1` to publish the sorter into `LargeFile.Sorter/Release`.
4. Run `file-sorter-run-10gb.ps1` to sort `LargeFiles/unsorted-10gb.txt` into `LargeFiles/sorted-10gb.txt`.

This gives you one complete large-file flow from generation to sorting with matching input and output names.

### Example Scenario: 100 GB

1. Run `file-generator-release.ps1` to publish the generator into `DummyFile.Generator/Release`.
2. Run `file-generator-run-100gb.ps1` to create `LargeFiles/unsorted-100gb.txt`.
3. Run `file-sorter-release.ps1` to publish the sorter into `LargeFile.Sorter/Release`.
4. Run `file-sorter-run-100gb.ps1` to sort `LargeFiles/unsorted-100gb.txt` into `LargeFiles/sorted-100gb.txt`.

This is the full high-volume scenario targeted by the assignment and uses the same runner layout as the smaller examples.

