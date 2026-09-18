# AGENTS.md

## Repository Overview

This repository contains two separate .NET console applications:

- `DummyFile.Generator/` generates large test files in the `<Number>. <String>` format.
- `LargeFile.Sorter/` sorts those files using external merge sort.

Each application has its own solution:

- `DummyFile.Generator/DummyFile.Generator.sln`
- `LargeFile.Sorter/LargeFile.Sorter.sln`

Useful top-level folders:

- `LargeFiles/` generated input files and final sorted outputs
- `Runners/` publish/run PowerShell scripts
- `temp/` sorter temporary chunk and intermediate merge files
- `PerformanceTesting/` helper scripts and log output for counters-based runs

Read `README.md` first for the assignment summary, design decisions, runners, and current TODO items.

## Build And Test

Preferred verification commands:

- `dotnet test LargeFile.Sorter\LargeFile.Sorter.sln`
- `dotnet test DummyFile.Generator\DummyFile.Generator.sln`

Published executables are launched through scripts in `Runners/`.

## Sorter Architecture

`LargeFile.Sorter` is the more complex project and uses:

- `HostApplicationBuilder`
- dependency injection
- structured logging
- explicit config objects registered in DI

Main pipeline phases:

1. Chunking
2. Multi-pass merge

### Chunking Phase

Key components:

- `SorterApplication`
- `InputFileReader`
- `ChunkSorterFactory`
- `ChunkSorter`
- `IChunkExecutionLimiter`
- `IChunkingProgressReporter`

Important behavior:

- Source reading is currently single-producer.
- `InputFileReader` reads the source file sequentially and fills one chunk at a time.
- Chunk sorting overlaps with reading, but producer throughput can still be the bottleneck.
- Do not assume low active sorter count means the limiter is too strict.
- `ChunkExecutionLimiter` is heuristic-based: `min(cpu, memory / (chunkSize * 2))`.

Current default chunk size:

- `128 MB`

### Merge Phase

Key components:

- `MergeSortingCoordinator`
- `MergeBatchProcessorFactory`
- `MergeBatchProcessor`
- `IMergeExecutionLimiter`
- `IMergeProgressReporter`
- `IMergeBatchProgressReporter`

Important behavior:

- Merge is parallelized only at the batch level.
- One batch still performs a sequential k-way merge with `PriorityQueue`.
- `MergeSortingCoordinator` is intentionally simple and effectively single-threaded from the caller point of view.
- Do not reintroduce a shared singleton `MergeBatchProcessor`.
- Each merge batch must get its own `MergeBatchProcessor` from `MergeBatchProcessorFactory`.
- `MergeBatchProcessorFactory` currently assembles the batch-scoped graph:
  - merge output adapter
  - merge output writer
  - merge batch progress reporter
  - input temp readers array
- `MergeBatchProcessor` is an executor over already prepared batch-scoped resources.

Current default merge settings:

- `MergeConfig.MaxChunkFilesPerMerge = 64`
- `MergeConfig.MaxConcurrentMergeBatches = 4`

## Temp File Lifecycle

The sorter relies heavily on temp-file abstractions:

- `ITempFileAdapter`
- `ITempFileWriter`
- `ITempFileReader`

Current rules:

- adapters own their streams
- adapters complete and dispose read/write streams
- temp files are deleted on adapter disposal
- final merge output is promoted to the destination before the final temp adapter is disposed

Be careful when refactoring lifecycle code:

- disposing too early can delete files that are still needed
- failed merge batches should still clean up partial temp output

## Progress Reporting

Progress reporting exists in three places:

- `ChunkingProgressReporter`
- `MergeProgressReporter`
- `MergeBatchProgressReporter`

Conventions:

- periodic logs are passive and centralized in reporters
- orchestration components report events but do not own timers
- elapsed time is intentionally formatted as `hh:mm:ss`
- merge batch progress uses written output bytes as the practical progress metric

If you add more progress logs, keep them compact. The console output is already busy during long runs.

## Performance Notes

Known hotspots and caveats:

- Chunking is often producer-limited by source reading and chunk assembly logic.
- Merge throughput depends heavily on fan-in. A `64-way` merge can look much slower than a `2-way` merge without indicating a regression.
- `write speed` in merge progress logs is effective output throughput, not raw disk capability.
- Temp and output files on the same physical disk can reduce throughput because reads and writes compete with each other.

When investigating regressions:

- compare the same phase/pass/batch shape before and after a change
- do not compare `pass 1, 64 files` against `pass 2, 2 files`
- prefer full wall-clock comparison for the same scenario size

## Logging Notes

- The default console logger shows full category names.
- Some future cleanup may prefer shorter logger categories for very noisy progress classes.
- Runners can selectively enable debug logs via environment variables.

## Runners And Memory Limits

Sorter runner scripts commonly set:

- `DOTNET_GCHeapHardLimit`

This is a managed heap cap, not a guarantee that the process will consume that much memory.

`Runners/file-sorter-run-10gb.ps1` is useful for manual testing and has been used to inspect merge debug logs.

## Current Sorter TODO Direction

The current `README.md` TODO list for the sorter is the source of truth, but the most important active directions are:

- dynamic load balancing
- binary sorting / binary row format optimization

## Safe Refactoring Guidance

Before changing sorter internals, check these invariants:

- chunk boundaries must remain newline-safe
- final sort order is `Text ASC`, then `Number ASC`
- duplicate rows are valid and must be preserved
- merge batching must continue to support multi-pass execution
- temp file cleanup must remain deterministic

When touching `LargeFile.Sorter`, run:

- `dotnet test LargeFile.Sorter\LargeFile.Sorter.sln`

When touching only docs or runner scripts, test execution is optional.
