module ConfigFP.WorkWithFiles
    open System
    open System.IO

    let private PathToExecutableProject = Environment.CurrentDirectory |> Path.GetDirectoryName |> Path.GetDirectoryName 

    let GetDBName pPath= 
        (Path.GetFileName pPath).Split '-' |> Array.head

    let private ReadFile pFilePath= 
        Path.Combine(PathToExecutableProject, "PS", pFilePath) |> File.ReadAllText

    let GetFiles pPathFolderIIS pDatabaseFolder pPredicate = 
        let files = Path.Combine(pPathFolderIIS, pDatabaseFolder)
                      |> Directory.GetFiles 
                      |> Array.toList 
                      |> List.filter pPredicate

        match files.Length > 0 with
        | true -> Some files
        | false -> None

    let  PredicateForPatchedNot (z:string) = (z.Contains(".bacpac") && not (z.Contains("-patched")))
    let  PredicateForPatched (z:string) = (z.Contains(".bacpac") && (z.Contains("-patched")))

    let ProcessScript f pScriptName = f (ReadFile pScriptName)