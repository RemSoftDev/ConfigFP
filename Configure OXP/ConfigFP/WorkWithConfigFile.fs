module ConfigFP.WorkWithConfigFile

open WorkWithFiles
open Newtonsoft.Json

    let ConvertToRecord s = new JsonSerializerSettings(TypeNameHandling = TypeNameHandling.All)
    let Init = 
        ProcessFileContent 
        ""
