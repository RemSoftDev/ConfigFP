module ConfigFP.Types.Types

open System.Net

    type ConfigJsonPathsState =
        { IIS : string
          GIT : string
          ConfigPowerShell : string}

    type ConfigJsonDBState =
        { DbUser           : string
          DbPassword       : string
          DbServerName     : string
          DbDatabaseFolder : string}

    type ConfigJsonState = 
        { Path : ConfigJsonPathsState
          DB   : ConfigJsonDBState}

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