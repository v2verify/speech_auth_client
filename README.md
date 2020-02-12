# Dynamic.Speech.Authorization
A C# .Net client library that provides easy interaction with V2Verify Speech Authorization Cloud Services.

# Perform an enrollment using the Sample

    Dynamic.Speech.Samples.exe enroll --developer-key <developer-key> --application-key <application-key> --client-id <client-id> --path <path/to/client-id/enrollment/files>

    Required Arguments
        --developer-key V2Verify DeveloperKey
        --application-key V2Verify ApplicationKey
        --client-id a unique client identifier from 3 to 64 alpha-numeric characters
        --path the path to the client-id enrollment files

    Optional Arguments
        --gender client-id sub-population male|female|unknown
        --interaction-id external tracking id
        --interaction-tag external tracking tag

    Note
        All arguments are case-sensitive and are lower-case

# Perform a verification using the Sample

    Dynamic.Speech.Samples.exe verify --developer-key <developer-key> --application-key <application-key> --client-id <client-id> --path <path/to/client-id/verify/files>

    Required Arguments
        --developer-key V2Verify DeveloperKey
        --application-key V2Verify ApplicationKey
        --client-id a unique client identifier from 3 to 64 alpha-numeric characters
        --path the path to the client-id enrollment files

    Optional Arguments
        --interaction-id external tracking id
        --interaction-tag external tracking tag

    Note
        All arguments are case-sensitive and are lower-case
