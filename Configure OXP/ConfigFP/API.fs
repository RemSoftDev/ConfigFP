namespace ConfigFP

open Types.Types
open Extentions.Extentions

module private WorkWithFiles =
    open System
    open System.IO

    let private PathToExecutableProject = Environment.CurrentDirectory |> Path.GetDirectoryName |> Path.GetDirectoryName 

    let GetDBName pFilePath= 
        (Path.GetFileName pFilePath).Split '-' |> Array.head

    let private ReadFile pFilePath= 
        Path.Combine(PathToExecutableProject, "PS", pFilePath) |> File.ReadAllText

    let private GetFileNames pPathFolderIIS pDatabaseFolder pPredicate = 
        Some(Path.Combine(pPathFolderIIS, pDatabaseFolder) |> Directory.GetFiles |> Array.toList |> List.filter pPredicate)

    let PoverShellFile_RemoveMasterKeyLT4GB = ReadFile "RemoveMasterKeyLT4GB.ps1"
    let PoverShellFile_ImportDataTierLayer = ReadFile "ImportDataTierLayer.ps1"

    let private PredicateForPatchedNot (z:string) = (z.Contains(".bacpac") && not (z.Contains("-patched")))
    let private PredicateForPatched (z:string) = (z.Contains(".bacpac") && (z.Contains("-patched")))

    let GetFilesNamesPatched pPathFolderIIS pDatabaseFolder =
         GetFileNames pPathFolderIIS pDatabaseFolder PredicateForPatched
    
    let GetFilesNamesPatchedNot pPathFolderIIS pDatabaseFolder =
         GetFileNames pPathFolderIIS pDatabaseFolder PredicateForPatchedNot

open WorkWithFiles

module DB =
    let GetConnectionString 
            pDbUser 
            pDbPassword
            pDbServerName
            pFileName =

         sprintf "user id=%s;password=%s;Data Source=%s;Database=%s; Integrated Security=false;" 
            pDbUser 
            pDbPassword
            pDbServerName 
            pFileName

    let Init = 
        ""

open DB

module WorkWithPowerShell =
    open System.Management.Automation
    open System.Collections

    let private RunPowerShell pScript (pParams:IDictionary)=
        (
        use PowerShellInstance = 
            PowerShell.
                Create().
                AddScript(pScript).
                AddParameters(pParams)

        (Some(PowerShellInstance.Invoke()), pParams)
        )

    let RunImport pGetConnectionStringCurry pScript pPathBacpac=
         let connStr = pGetConnectionStringCurry (GetDBName pPathBacpac)
         let dict = new System.Collections.Generic.Dictionary<string,string>() 
         dict.["bacpacPath"] <- pPathBacpac
         dict.["connectionString"] <- connStr
         RunPowerShell pScript dict

    let RunPatch pScript pPathBacpac =
         let dict = new System.Collections.Generic.Dictionary<string,string>()
         dict.["bacpacPath"] <- pPathBacpac
         RunPowerShell pScript dict

open WorkWithPowerShell

module Validate = 
    open System.Collections

    let Valid pText pUpdateUI (z,x:IDictionary) =
        let str = pText + " " + x.Item("bacpacPath").ToString();

        pUpdateUI (match z with
                    | Some c -> "Succesfully " + str
                    | None -> "Error" + str)
        
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
        let RunPatchCurry = RunPatch PoverShellFile_RemoveMasterKeyLT4GB
        let ValidCurry = Valid "Patch" pState.UpdateUI
        GetFilesNamesPatchedNot pState.PathFolderIIS pState.DatabaseFolder 
            |> Option.map (List.map RunPatchCurry)
            |> Option.map (List.map ValidCurry) 
            |> ignore
        pState
    
    let ImportBacPacs pState = 
        let ConnStrCurry = GetConnectionString pState.DbUser pState.DbPassword pState.DbServerName
        let RunImportCurry = RunImport ConnStrCurry PoverShellFile_ImportDataTierLayer
        let ValidCurry = Valid "Patch" pState.UpdateUI

        GetFilesNamesPatched pState.PathFolderIIS pState.DatabaseFolder 
            |> Option.map (List.map RunImportCurry)
            |> Option.map (List.map ValidCurry) 
            |> ignore
        pState
