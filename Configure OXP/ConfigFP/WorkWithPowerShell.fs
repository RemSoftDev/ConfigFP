module ConfigFP.WorkWithPowerShell
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
   
    let RunImport pGetDBName pGetConnectionStringCurry pScript pPathBacpac =
         let dict = new System.Collections.Generic.Dictionary<string,string>() 
         dict.["bacpacPath"] <- pPathBacpac
         dict.["connectionString"] <- pGetConnectionStringCurry (pGetDBName pPathBacpac)
         RunPowerShellAsync pScript dict |> Async.RunSynchronously

    let RunPatch pScript pBacpac =
         let dict = new System.Collections.Generic.Dictionary<string,string>()
         dict.["bacpacPath"] <- pBacpac
         RunPowerShellAsync pScript dict |> Async.RunSynchronously

