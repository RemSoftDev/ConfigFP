namespace ConfigFP

open Types.Types
open Extentions.Extentions

module CustomInfixOperators = 
    let (||>>) opt f = opt |> Option.map f 
    let (|||) f g = (fun opt -> (opt ||>> f) ||>> g)


module Validate = 
    open System.Collections

    let Valid pText pUpdateUI (z:bool, x:IDictionary) =
        let str = pText + " " + x.Item("bacpacPath").ToString();

        pUpdateUI (match z with
                    | true -> "Succesfully " + str
                    | false -> "Error" + str)
      
open Validate
open CustomInfixOperators

module API =
    open WorkWithFiles
    open WorkWithPowerShell
    open WorkWithDB

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
        let ValidCurry = Valid "Patch" pState.UpdateUI
        let res = pBacpacList
                 ||>> List.map pScriptForApply
                 ||>> List.map ValidCurry
 
        let h = ""
        pState
    
    let ImportBacPacs pState = 
        let ConnStrCurry = GetConnectionString pState.DbUser pState.DbPassword pState.DbServerName
        let pBacpacList =  GetFiles pState.PathFolderIIS pState.DatabaseFolder PredicateForPatched
        let pScriptForApply = "ImportDataTierLayer.ps1" |> ProcessScript (RunImport GetDBName ConnStrCurry)
        let ValidCurry = Valid "Import" pState.UpdateUI
        let res = pBacpacList 
                 ||>> List.map pScriptForApply
                 ||>> List.map ValidCurry
        let h = ""
        pState


        
