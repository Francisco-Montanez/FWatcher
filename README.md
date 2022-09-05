# FWatcher

Watch directories and perform action(s) on changes.

## Installation

Add as nuget package

```fs
dotnet add package FWatcher
```

## Using FWatcher

```fs
module App

open System
open FSharp.Interop.Excel


type WatcherGrid = ExcelFile<FileName="../example/my_grid.xlsx",HasHeaders=true,ForceString=true> // use excel file

let myGrid = new WatcherGrid()


open FWatcher // open FWatcher


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
        |> Async.Ignore // ignore results
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

let action watcher directoryChanges = // action performed on changes
  async {
    prettyPrint directoryChanges
    return ()
  }

[<EntryPoint>]
let main _ =
  async {
    do! startWatchers action myGrid
    return 0
  }
  |> Async.RunSynchronously
```
