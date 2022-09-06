# FWatcher

Watch directories and perform actions when changes are detected.

## Installation

Add it as a nuget package

```fs
dotnet add package FWatcher
```

## How It Works

We need
  - `Path`: at least one directory path to 'watch'
  - `Pattern`: regex pattern of files to keep track of

The `Watcher` type will hold that data.

```fs
type Watcher = { Path: string; Pattern: string }
```

We make a `DirectoryState` by hashing the contents inside `Watcher.Path` that conform to `Watcher.Pattern`.

The kv pairs are the file paths and their hashed values.

```fs
type DirectoryState = Map<string, byte[]>
```

We get `DirectoryChanges` when comparing `DirectoryState`'s. If any changes exist, we can trigger an action. (explained further below)

```fs
type DirectoryChanges =
  {
    Count: int
    Added: DirectoryState
    Deleted: DirectoryState
    Modified: DirectoryState
  }
```

To start 'watching' a directory we call the `watch` function.

```fs
let watch (action: Watcher -> DirectoryChanges -> Async<unit>) (compareStateInterval: int) (watcher: Watcher)
```


`watch` expects

  - **action**: what action to take when there are directory changes

  - **compareStateInterval**: how often the directory will be checked for changes, in milliseconds

  - **watcher**: directory path and regex pattern to look for

## How to Use

```fs
module Example

open System
open FWatcher

module SimpleExample =

  let watcher = { Path = "../Example/watching"; Pattern = ".*.txt"}

  let someActionOnChanges watcher directorychanges =
    async {
      printfn "Changes in %s" watcher.Path
      printfn "Added: %A" directorychanges.Added.Keys
      printfn "Deleted: %A" directorychanges.Deleted.Keys
      printfn "Modified: %A" directorychanges.Modified.Keys
      return ()
    }

module ExcelExample =
  open FSharp.Interop.Excel

  type WatcherGrid = ExcelFile<FileName = "../Example/my_grid.xlsx", HasHeaders = true, ForceString = true>

  let myGrid = new WatcherGrid()

  let prettyPrint directoryChanges =
    let prettyishPrint change keys =
      let s = sprintf "\n\n\t%s\n\t\t%s\n" change
      [ for k in keys do yield s k ]
      |> String.concat ""

    match directoryChanges with
    | directoryChanges when directoryChanges.Count > 0 ->
      printfn "\nChanges detected:%s%s%s"
        (prettyishPrint "Added:" directoryChanges.Added.Keys)
        (prettyishPrint "Deleted:" directoryChanges.Deleted.Keys)
        (prettyishPrint "Modified:" directoryChanges.Modified.Keys)
    | o -> ()

  let prettyPrintOnChange watcher directoryChanges =
    async {
      prettyPrint directoryChanges
      return ()
    }

  let startWatchers () =
    async {
      try
        return!
          myGrid.Data
          |> Seq.filter (fun row -> not (String.IsNullOrWhiteSpace row.Path || String.IsNullOrWhiteSpace row.Pattern))
          |> Seq.map (fun row -> { Path = row.Path; Pattern = row.Pattern })
          |> Seq.map (watch prettyPrintOnChange 500)
          |> Async.Parallel
          |> Async.Ignore
      with ex -> printfn $"%s{ex.Message}"; return ()
    }

[<EntryPoint>]
let main _ =
  async {
    // do! FWatcher.watch SimpleExample.someActionOnChanges 1000 SimpleExample.watcher
    do! ExcelExample.startWatchers ()
    return 0
  }
  |> Async.RunSynchronously
```
