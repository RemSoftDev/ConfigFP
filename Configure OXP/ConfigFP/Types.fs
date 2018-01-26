module ConfigFP.Types.Types

    type ConfigState = 
        { PathFolderIIS          : string
          PathFolderGIT          : string
          DbUser                 : string
          DbPassword             : string
          DbServerName           : string
          FilesNamesPatched      : string[] option
          FilesNamesPatchedNot   : string[] option    
          UpdateUI               : string -> string -> string       
          DatabaseFolder         : string}

    type Result<'TSuccess, 'TMessage> = 
        | Ok of 'TSuccess * 'TMessage list
        | Bad of 'TMessage list