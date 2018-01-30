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

    let ApplyScriptToEveryFile pBacpacList f =        
        pBacpacList |> Option.map (List.map f)
    
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
         let dict = new System.Collections.Generic.Dictionary<string,string>() 
         dict.["bacpacPath"] <- pPathBacpac
         dict.["connectionString"] <- pGetConnectionStringCurry (GetDBName pPathBacpac)
         RunPowerShell pScript dict

    let RunPatch pScript pBacpac =
         let dict = new System.Collections.Generic.Dictionary<string,string>()
         dict.["bacpacPath"] <- pBacpac
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
        
        let pBacpacList =  GetFiles pState.PathFolderIIS pState.DatabaseFolder PredicateForPatchedNot
        let pScriptForApply = "RemoveMasterKeyLT4GB.ps1" |> ProcessScript RunPatch
        ApplyScriptToEveryFile pBacpacList pScriptForApply
        |> ignore
        pState
    
    let ImportBacPacs pState = 
        let ConnStrCurry = GetConnectionString pState.DbUser pState.DbPassword pState.DbServerName
        let pBacpacList =  GetFiles pState.PathFolderIIS pState.DatabaseFolder PredicateForPatched
        let pScriptForApply = "ImportDataTierLayer.ps1" |> ProcessScript (RunImport ConnStrCurry)
        ApplyScriptToEveryFile pBacpacList pScriptForApply
        |> ignore
        pState


        
