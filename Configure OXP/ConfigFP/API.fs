namespace ConfigFP

open Types.Types
open Extentions.Extentions

module private WorkWithFiles =
    type Monoid<'T> = { identity : 'T; reducer : ('T -> 'T -> 'T); } 
    let retM identity reducer = { identity = identity; reducer = reducer; }
    let reduce elements monoid = 
        (elements @ [monoid.identity; monoid.identity;]) 
        |> List.reduce monoid.reducer

    open System
    open System.IO

    let (||>>) opt f = opt |> Option.map f 
    let (|||) f g = (fun opt -> (opt ||>> f) ||>> g)

    type Example = 
    | Add of (int * int)
    | SideEffect of (int * (int -> int))
    | Wire of (Example * (int -> Example))

    let rec execute e = 
        match e with
        | Add (l, r) -> l + r
        | SideEffect (arg, f) -> arg |> f
        | Wire (e, f) -> e |> execute |> f |> execute 

    let (<.>) e f = Wire(e, f)

    let makeSideEffect f x = 
        SideEffect(x, (fun x' -> 
            f x'
            x'))
        
    Add(1, 1) <.> (makeSideEffect (printf "%i")) |> execute

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
open DB

module WorkWithPowerShell =
    open System.Management.Automation
    open System.Collections
    open System

    type Test1() =
          static member add (a) b=
               ()

    let handler = new EventHandler<DataAddedEventArgs>(Test1.add)

    let private RunPowerShellAsync pScript (pParams:IDictionary)=
        async {           
                use powerShellInstance = 
                       PowerShell.
                        Create().
                        AddScript(pScript).
                        AddParameters(pParams)
            
                let output=new PSDataCollection<PSObject>()
                output.DataAdded.AddHandler(handler)

                let beginInvoke = powerShellInstance.BeginInvoke<PSObject, PSObject>(null, output)
                let! returnValue = Async.AwaitIAsyncResult(beginInvoke)
                let endInvoke = powerShellInstance.EndInvoke(beginInvoke)

                return (output.Count > 0, pParams)
        }
   
    let RunImport pGetConnectionStringCurry pScript pPathBacpac=
         let dict = new System.Collections.Generic.Dictionary<string,string>() 
         dict.["bacpacPath"] <- pPathBacpac
         dict.["connectionString"] <- pGetConnectionStringCurry (GetDBName pPathBacpac)
         RunPowerShellAsync pScript dict |> Async.RunSynchronously

    let RunPatch pScript pBacpac =
         let dict = new System.Collections.Generic.Dictionary<string,string>()
         dict.["bacpacPath"] <- pBacpac
         RunPowerShellAsync pScript dict |> Async.RunSynchronously

open WorkWithPowerShell

module Validate = 
    open System.Collections
    open System.Management.Automation

    let Valid pText pUpdateUI (z:bool, x:IDictionary) =
        let str = pText + " " + x.Item("bacpacPath").ToString();

        pUpdateUI (match z with
                    | true -> "Succesfully " + str
                    | false -> "Error" + str)
        
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
        let ValidCurry = Valid "Patch" pState.UpdateUI
        let res = pBacpacList
                 ||>> List.map pScriptForApply
                 ||>> List.map ValidCurry
 
        let h = ""
        pState
    
    let ImportBacPacs pState = 
        let ConnStrCurry = GetConnectionString pState.DbUser pState.DbPassword pState.DbServerName
        let pBacpacList =  GetFiles pState.PathFolderIIS pState.DatabaseFolder PredicateForPatched
        let pScriptForApply = "ImportDataTierLayer.ps1" |> ProcessScript (RunImport ConnStrCurry)
        let ValidCurry = Valid "Import" pState.UpdateUI
        let res = pBacpacList 
                 ||>> List.map pScriptForApply
                 ||>> List.map ValidCurry
        let h = ""
        pState


        
