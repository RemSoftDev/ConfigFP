namespace ConfigFP

open Types.Types
open Extentions.Extentions

module private WorkWithFiles =
    open System
    open System.IO

    let private PathToExecutableProject = Environment.CurrentDirectory |> Path.GetDirectoryName |> Path.GetDirectoryName 

    let GetFileName pFilePath= 
        Path.GetFileName pFilePath

    let private ReadFile pFilePath= 
        Path.Combine(PathToExecutableProject, "PS", pFilePath) |> File.ReadAllText

    let private GetFileNames pPathFolderIIS pDatabaseFolder pPredicate = 
        Some(Path.Combine(pPathFolderIIS, pDatabaseFolder) |> Directory.GetFiles |> Array.toList |> List.filter pPredicate)

    let PoverShellFile_RemoveMasterKeyLT4GB = ReadFile "RemoveMasterKeyLT4GB.ps1"
    let private PoverShellFile_ImportDataTierLayer = ReadFile "ImportDataTierLayer.ps1"

    let private PredicateForPatchedNot (z:string) = (z.Contains(".bacpac") && not (z.Contains("-patched")))
    let private PredicateForPatched (z:string) = (z.Contains(".bacpac") && (z.Contains("-patched")))

    let GetFilesNamesPatched pPathFolderIIS pDatabaseFolder =
         GetFileNames pPathFolderIIS pDatabaseFolder PredicateForPatched
    
    let GetFilesNamesPatchedNot pPathFolderIIS pDatabaseFolder =
         GetFileNames pPathFolderIIS pDatabaseFolder PredicateForPatchedNot

open WorkWithFiles

module DB =
    let Init = 
        ""

module WorkWithPowerShell =
    open System.Management.Automation
    open System.Collections

    //let Init pPathFolderIIS pPathFolderGIT =
    //    { PathFolderIIS          = pPathFolderIIS
    //      PathFolderGIT          = pPathFolderIIS}
          

    let private RunPowerShell pScript (pParams:IDictionary)=
        (
            use PowerShellInstance = 
                PowerShell.
                    Create().
                    AddScript(pScript).
                    AddParameters(pParams)

            (Some(PowerShellInstance.Invoke()), pParams)
        )

    let RunImport pFile=
        

        ""

    let RunPatch pScript pPathBacpac =
         let dict = new System.Collections.Generic.Dictionary<string,string>()
         dict.["bacpacPath"] <- pPathBacpac
         RunPowerShell pScript dict

open WorkWithPowerShell

module Validate = 
    open System.Collections

    let ValidPatch pUpdateUI (z,x:IDictionary) =
        pUpdateUI (match z with
                    | Some c -> "Succesfully patched "+ x.Item("bacpacPath").ToString()
                    | None -> "Error")
        
open Validate

module API =
    open Microsoft.FSharp

    let Init 
            pDbUser 
            pDbPassword 
            pDbServerName 
            pPathFolderIIS 
            pPathFolderGIT 
            (pUpdateUI:System.Func<string, string>) =

        {PathFolderIIS = pPathFolderIIS
         PathFolderGIT = pPathFolderGIT
         DbUser = pDbUser
         DbPassword = pDbPassword
         DbServerName = pDbServerName
         UpdateUI = pUpdateUI.ToFSharpFunc()
         DatabaseFolder = "Databases"}

    let PatchBacPacs pState = 
        GetFilesNamesPatchedNot pState.PathFolderIIS pState.DatabaseFolder 
            |> Option.map (List.map (RunPatch PoverShellFile_RemoveMasterKeyLT4GB))
            |> Option.map (List.map (ValidPatch pState.UpdateUI)) 
            |> ignore
        pState

    
    let ImportBacPacs pState = 
        
        ""

