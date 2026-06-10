# ca-surveillance-test-task

## Test Assignment

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

## Runners

Launch scripts live in the root `Runners/` folder.

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

### Example Scenario: 10 GB

1. Run `file-generator-release.ps1` to publish the generator into `DummyFile.Generator/Release`.
2. Run `file-generator-run-10gb.ps1` to create `LargeFiles/unsorted-10gb.txt`.
3. Run `file-sorter-release.ps1` to publish the sorter into `LargeFile.Sorter/Release`.
4. Run `file-sorter-run-10gb.ps1` to sort `LargeFiles/unsorted-10gb.txt` into `LargeFiles/sorted-10gb.txt`.

This gives you one complete large-file flow from generation to sorting with matching input and output names.

### Dummy File Generator TODO

- Add multithreaded generation.
- Refactor text generation from ASCII-specific logic to proper UTF-8 support.
- Reduce per-row allocations in `ItemsGenerator` to improve generation throughput.
