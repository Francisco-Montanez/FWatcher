module FWatcher

open System
open System.IO


module Async =

  let map f op =
    async {
      let! x = op
      return f x
    }

type DirectoryChanges = { Count: int; Added: Map<string, byte array>; Deleted: Map<string, byte array>; Modified: Map<string, byte array> }

type Watcher = { PathToWatch: string; Pattern: string }


/// <summary>Gets the state of a directory</summary>
/// <param name="dir_path">directory path</param>
/// <param name="pattern">pattern to look for</param>
let getDirectoryState dir_path pattern =
  async {
    try
      let regex = new Text.RegularExpressions.Regex(pattern)

      let files =
        dir_path
        |> Directory.EnumerateFiles
        |> Seq.filter regex.IsMatch

      let! hash =
        files
        |> Seq.map (File.ReadAllBytesAsync >> Async.AwaitTask)
        |> Async.Parallel
        |> Async.map (Seq.map (Security.Cryptography.SHA256.Create().ComputeHash))

      return
        Seq.zip files hash
        |> Map.ofSeq

    with ex -> printfn $"%s{ex.Message}"; return Map.empty
  }

/// <summary>Compare two directory states</summary>
/// <param name="prev">previous directory state</param>
/// <param name="curr">current directory state</param>
let compareState prev curr =
  let modified =
    seq {
      for KeyValue(k, cv) in curr do
        match Map.tryFind k prev with
        | Some pv when cv <> pv -> yield k, cv
        | Some pv -> ()
        | None -> ()
    }
    |> Map

  let added =
    seq {
      for KeyValue(k, cv) in curr do
        match Map.tryFind k prev with
        | Some pv -> ()
        | None -> k, cv
    }
    |> Map

  let deleted =
    seq {
      for KeyValue(k, pv) in prev do
        match Map.tryFind k curr with
        | Some cv -> ()
        | None -> yield k, pv
    }
    |> Map

  {
    Count = added.Count + deleted.Count + modified.Count
    Added = added
    Deleted = deleted
    Modified = modified
  }

/// <summary>Watch a directory for additions, deletions, modifications</summary>
/// <param name="action">action to perform when change is detected</param>
/// <param name="compareStateInterval">how often directory changes should be updated, in milliseconds</param>
/// <param name="watcher">watcher type containing the path to watch and pattern to look for</param>
let watch action (compareStateInterval: int) watcher =
  async {
    try
      let! prev = getDirectoryState watcher.PathToWatch watcher.Pattern
      let mutable prev' = prev

      while true do
        do! Async.Sleep compareStateInterval

        let! curr = getDirectoryState watcher.PathToWatch watcher.Pattern

        let changes = compareState prev' curr

        if changes.Count > 0 then do! action watcher changes

        prev' <- curr

      return ()

    with ex -> printfn $"%s{ex.Message}"; return ()
  }
