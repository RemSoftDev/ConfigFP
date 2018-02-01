module WorkWithDB
    let GetConnectionString pU pP pS pD =
         sprintf "user id=%s;password=%s;Data Source=%s;Database=%s; Integrated Security=false;" pU pP pS pD