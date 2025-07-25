string endpoint = context.Request.Path.Value?
    .Trim('/')
    .Split('/')
    .LastOrDefault() ?? "UnknownEndpoint";
