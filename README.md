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

## Dummy File Generator Design Decisions

- The File Generator console application is intentionally kept as simple as possible. Because of that, it does not use the standard .NET application infrastructure such as Host Builder, Dependency Injection containers, or configuration binding from multiple sources.
- File system access is isolated behind dedicated adapter abstractions `IFileAdapter`. This keeps file creation and stream opening concerns separated from the higher-level export workflow `IFileExporter` and makes that logic easier to test in isolation.
- Data generation is separated from file export. This allows the generation pipeline and the disk writing pipeline to be verified independently and makes future changes safer and more localized.
- `IRowFormatter` was introduced intentionally as a separate abstraction so the output row format can evolve without coupling that change to the generator or exporter. For example, the separator could be changed from `". "` to `","` in the future with minimal impact on the rest of the application.

### Dummy File Generator TODO

- Add a progress bar.
- Add multithreaded generation.
- Refactor text generation from ASCII-specific logic to proper UTF-8 support.
