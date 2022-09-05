module Example

open System
open FSharp.Interop.Excel


type WatcherGrid = ExcelFile<FileName = "../example/my_grid.xlsx", HasHeaders = true, ForceString = true>

let myGrid = new WatcherGrid()


open FWatcher


let getWatchers (grid: WatcherGrid) =
  grid.Data
  |> Seq.filter (fun row -> not (String.IsNullOrWhiteSpace row.Path || String.IsNullOrWhiteSpace row.Pattern))
  |> Seq.map (fun row -> { Path = row.Path; Pattern = row.Pattern })
  |> Seq.toList

let startWatchers action grid =
  async {
    try
      return!
        grid
        |> getWatchers
        |> List.map (watch action 500)
        |> Async.Parallel
        |> Async.Ignore
    with ex -> printfn $"%s{ex.Message}"; return ()
  }

let prettyprint s (m:Map<string,byte[]>) =
  for i in m do
    printfn "\t%s: %A\n" s i.Key

let action watcher directorychanges =
  async {
    if directorychanges.Count > 0
    then printfn "Changes detected:\n"
    if directorychanges.Added.Count > 0
    then
      prettyprint "Added" directorychanges.Added
    if directorychanges.Deleted.Count > 0
    then
      prettyprint "Deleted" directorychanges.Deleted
    if directorychanges.Modified.Count > 0
    then
      prettyprint "Modified" directorychanges.Modified
    return ()
  }

[<EntryPoint>]
let main _ =
  async {
    do! startWatchers action myGrid
    return 0
  }
  |> Async.RunSynchronously
