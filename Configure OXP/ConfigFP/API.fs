namespace ConfigFP

open Types.Types
open Extentions.Extentions

module private WorkWithFiles =
    open System
    open System.IO

    let private PathToExecutableProject = Environment.CurrentDirectory |> Path.GetDirectoryName |> Path.GetDirectoryName 

    let GetFileName pFilePath= 
        Path.GetFileName pFilePath

    let ReadFile pFilePath= 
        Path.Combine(PathToExecutableProject, "PS", pFilePath) |> File.ReadAllText

    let private GetFileNames pConfigState pPredicate = 
        Path.Combine(pConfigState.PathFolderIIS, pConfigState.DatabaseFolder) |> Directory.GetFiles |> Array.filter pPredicate 

    let private PoverShellFile_RemoveMasterKeyLT4GB = ReadFile "RemoveMasterKeyLT4GB.ps1"
    let private PoverShellFile_ImportDataTierLayer = ReadFile "ImportDataTierLayer.ps1"

    let private PredicateForPatchedNot (z:string) = (z.Contains(".bacpac") && not (z.Contains("-patched")))
    let private PredicateForPatched (z:string) = (z.Contains(".bacpac") && (z.Contains("-patched")))

    let private GetFilesNamesPatched pConfigState =
        {pConfigState with FilesNamesPatched = Some(GetFileNames pConfigState PredicateForPatched)}
    
    let private GetFilesNamesPatchedNot pConfigState =
        {pConfigState with FilesNamesPatchedNot = Some(GetFileNames pConfigState PredicateForPatchedNot)}

module private WorkWithPowerShell =
    open System.Management.Automation
    open System.Collections

    let RunImport =
        //let colors = dict["blue", 40; "red", 700]
        ""

    let RunPatch pPath =
        ""

    let private RunPowerShell pScript (pParams:IDictionary)=
        (
            use PowerShellInstance = 
                PowerShell.
                    Create().
                    AddScript(pScript).
                    AddParameters(pParams)
          
            Some(PowerShellInstance.Invoke()) 
        )

    let Patch pPathToBacpac =
        pPathToBacpac
        |> Array.Parallel.map RunPatch

    let NotNull x = 
        if x = null
            then None
        else Some x

    let PatchDBs pPathToBacpacOption =
        ""

module API =
    let Init 
        pDbUser 
        pDbPassword 
        pDbServerName 
        pPathFolderIIS 
        pPathFolderGIT 
        (pUpdateUI:System.Func<string, string, string>) =

        {PathFolderIIS = pPathFolderIIS
         PathFolderGIT = pPathFolderGIT
         DbUser = pDbUser
         DbPassword = pDbPassword
         DbServerName = pDbServerName
         FilesNamesPatched = None
         FilesNamesPatchedNot = None
         UpdateUI = pUpdateUI.ToFSharpFunc()
         DatabaseFolder = "Databases"}

