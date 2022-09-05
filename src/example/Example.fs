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

let action watcher directoryChanges =
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
