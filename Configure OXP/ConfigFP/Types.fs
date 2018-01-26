module ConfigFP.Types

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

