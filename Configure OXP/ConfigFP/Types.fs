module ConfigFP.Types.Types
 
    type FilesState = 
        { PathFolderIIS          : string
          PathFolderGIT          : string
          DatabaseFolder         : string}
 
    type PSState = 
        { PathFolderIIS          : string
          PathFolderGIT          : string
          FilesNamesPatched      : string[] option
          FilesNamesPatchedNot   : string[] option }

    type ConfigJsonPathsState =
        { IIS : string
          GIT : string}

    type ConfigJsonState = 
        { Path                   : ConfigJsonPathsState list
          DbUser                 : string
          DbPassword             : string
          DbServerName           : string
          DatabaseFolder         : string}

    type ConfigState = 
        { PathFolderIIS          : string
          PathFolderGIT          : string
          DbUser                 : string
          DbPassword             : string
          DbServerName           : string
          UpdateUI               : string -> string       
          DatabaseFolder         : string}

    type Result<'TSuccess, 'TMessage> = 
        | Ok of 'TSuccess * 'TMessage list
        | Bad of 'TMessage list