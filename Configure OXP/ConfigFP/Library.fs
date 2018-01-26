namespace ConfigFP

open System.Security.Claims

module Types1 =

    type UiState = 
        | Start 
        | End 

module IO =
    open Types1
    open Types.Types
    open Extentions.Extentions
 
    let private UpdateUiForArray pConfigState (pPref, pArray) =
        pArray
        |> Array.map (pConfigState.UpdateUI pPref) 
        |> ignore

        pConfigState
    
    let private UpdateUi pConfigState pFiles pPref =                
        match pFiles with
            | Some x -> (pPref, x)
            | None -> ("Error", Array.create 1 "No elements for procesing")    
        |> UpdateUiForArray pConfigState

    let UpdateUiPatched pUiState pConfigState=  
        let curry = UpdateUi pConfigState pConfigState.FilesNamesPatched
        match pUiState with
            | Start  -> curry "Start"
            | End -> curry "End"

    let UpdateUiPatchedNot pUiState pConfigState=             
        let curry = UpdateUi pConfigState pConfigState.FilesNamesPatchedNot
        match pUiState with
            | Start  -> curry "Start"
            | End -> curry "End"

open IO
        
module DB =
    open Types.Types
    open Types1
    open System
    open Extentions.Extentions
    open System.IO
    open System.Management.Automation
    open System.Collections.ObjectModel

    let private PathExecutableProj = Environment.CurrentDirectory |> Path.GetDirectoryName |> Path.GetDirectoryName 
    let private GetPoverShell_RemoveMasterKeyLT4GB = Path.Combine(PathExecutableProj, "PS", "RemoveMasterKeyLT4GB.ps1") |> File.ReadAllText
    let private GetPoverShell_ImportDataTierLayer = Path.Combine(PathExecutableProj, "PS", "ImportDataTierLayer.ps1") |> File.ReadAllText

    let private GetFileNames pConfigState pPredicate = 
        Path.Combine (pConfigState.PathFolderIIS, pConfigState.DatabaseFolder) |> Directory.GetFiles |> Array.filter pPredicate 

    let private PredicateForPatchedNot (z:string) = (z.Contains(".bacpac") && not (z.Contains("-patched")))
    let private PredicateForPatched (z:string) = (z.Contains(".bacpac") && (z.Contains("-patched")))
    
    let private GetFilesNamesPatched pConfigState =
        {pConfigState with FilesNamesPatched = Some(GetFileNames pConfigState PredicateForPatched)}
    
    let private GetFilesNamesPatchedNot pConfigState =
        {pConfigState with FilesNamesPatchedNot = Some(GetFileNames pConfigState PredicateForPatchedNot)}

    let private RunPatchScript pConfigState pFileName= 
        (
            use PowerShellInstance = 
                PowerShell.
                    Create().
                    AddScript(GetPoverShell_RemoveMasterKeyLT4GB).
                    AddParameter("bacpacPath", pFileName) 
            
            match Some(PowerShellInstance.Invoke()) with 
            | Some x -> pConfigState.UpdateUI "end" pFileName
            | None -> pConfigState.UpdateUI "error" pFileName             
        )
        
    let private PatchDB pConfigState =
        match pConfigState.FilesNamesPatchedNot with 
            | Some x -> x |> Array.Parallel.map (RunPatchScript pConfigState)
            | None -> Array.zeroCreate 1
            |> ignore
        pConfigState

    let Init 
            pDbUser 
            pDbPassword 
            pDbServerName 
            pPathFolderIIS 
            pPathFolderGIT 
            (pUpdateUI:System.Func<string, string, string>) =
        let pConfigState = 
            { PathFolderIIS = pPathFolderIIS
              PathFolderGIT = pPathFolderGIT
              DbUser = pDbUser
              DbPassword = pDbPassword
              DbServerName = pDbServerName
              FilesNamesPatched = None
              FilesNamesPatchedNot = None
              UpdateUI = pUpdateUI.ToFSharpFunc()
              DatabaseFolder = "Databases"}
        pConfigState
        |> GetFilesNamesPatchedNot
        |> GetFilesNamesPatched
    
    let PatchDBIfFilesFound pConfigState =
        pConfigState
        |> GetFilesNamesPatchedNot
        |> IO.UpdateUiPatched UiState.Start  
        |> PatchDB 
        |> GetFilesNamesPatched
        |> IO.UpdateUiPatchedNot UiState.End 

    let private GetDbName (pFileName:string)=
        let tmp = (pFileName |> Path.GetFileName).Split [|'-'|]
        tmp.[0]

    let private GetConnectionString pConfigState pFileName=
         sprintf "user id=%s;password=%s;Data Source=%s;Database=%s; Integrated Security=false;" 
            pConfigState.DbUser 
            pConfigState.DbPassword 
            pConfigState.DbServerName 
            (GetDbName pFileName)

    let private ImportPowerShell pConfigState pFileName=
        let conStr = GetConnectionString pConfigState pFileName
        (
            use PowerShellInstance = 
                PowerShell.
                    Create().
                    AddScript(GetPoverShell_ImportDataTierLayer).
                    AddParameter("bacpacPath", pFileName).
                    AddParameter("connectionString", conStr) 
          
            match Some(PowerShellInstance.Invoke()) with 
            | Some x -> pConfigState.UpdateUI "end" pFileName
            | None -> pConfigState.UpdateUI "error" pFileName        
        )


    let Import pConfigState =
        let ccc = match pConfigState.FilesNamesPatched with
        | Some x -> x |> Array.Parallel.map (ImportPowerShell pConfigState)
        | None -> Array.zeroCreate 1
        let v = ccc
        pConfigState







    
        
        
