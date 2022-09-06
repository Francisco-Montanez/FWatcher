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

### Example 1

```fs
module App

open FWatcher

let watcher = { Path = "../Example/watching"; Pattern = ".*.txt"}

let someActionOnChanges watcher directorychanges =
  async {
    printfn "Changes in %s" watcher.Path
    printfn "Added: %A" directorychanges.Added.keys
    printfn "Deleted: %A" directorychanges.Deleted.keys
    printfn "Modified: %A" directorychanges.Modified.keys
    return ()
  }

[<EntryPoint>]
let main _ =
  async {
    do! watch someActionOnChanges 1000 watcher
    return 0
  }
  |> Async.RunSynchronously
```

Example 2. Using Excel file

```fs
module App

open System
open FSharp.Interop.Excel


type WatcherGrid = ExcelFile<FileName="../Example/my_grid.xlsx",HasHeaders=true,ForceString=true> // use excel file

let myGrid = new WatcherGrid()


open FWatcher


let getWatchers (grid: WatcherGrid) =
  grid.Data // get grid data
  |> Seq.filter (fun row -> not (String.IsNullOrWhiteSpace row.Path || String.IsNullOrWhiteSpace row.Pattern)) // get non empty rows
  |> Seq.map (fun row -> { Path = row.Path; Pattern = row.Pattern }) // map to Watcher type
  |> Seq.toList // convert to list

let startWatchers action grid =
  async {
    try
      return!
        grid
        |> getWatchers // get Watcher types
        |> List.map (watch action 500) // run watch function with half second comparison intervals
        |> Async.Parallel // run watchers in parallel
        |> Async.Ignore // ignore results as they should never return unless they're stopped or errors occur
    with ex -> printfn $"%s{ex.Message}"; return ()
  }

let prettyPrint directoryChanges = // function that nicely prints directory changes
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

let prettyPrintOnChanges watcher directoryChanges = // action performed on changes
  async {
    prettyPrint directoryChanges
    return ()
  }

[<EntryPoint>]
let main _ =
  async {
    do! startWatchers prettyPrintOnChanges myGrid
    return 0
  }
  |> Async.RunSynchronously
```
