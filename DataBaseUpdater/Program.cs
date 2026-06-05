using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using NORCE.Drilling.Field.ModelShared;




string localHostName = "https://localhost:5001/";
string devHostName = "https://dev.digiwells.no/";
string fieldHostBase = "Field/api/";
string cartographicHostBase = "CartographicProjection/api/";


// Create clients to access databases from dev/ environment
Client fieldClient = ClientSetup(devHostName, fieldHostBase);
Client cartographicClient = ClientSetup(devHostName, cartographicHostBase);
// Create clients to access databases from local/ environment
Client fieldLocalClient = ClientSetup(localHostName, fieldHostBase);
Client cartographicLocalClient = ClientSetup(localHostName, cartographicHostBase);
// Get all fields and cartographic projection sets from the APIs
List<Field> fields = (List<Field>) (await fieldClient.GetAllFieldAsync()).ToList();
List<CartographicConversionSet> cartographicProjectionSets = (List<CartographicConversionSet>) (await cartographicClient.GetAllCartographicConversionSetAsync()).ToList();

// Update local database with data from dev/ database
foreach (var field in fields)
{
    // Update each field in local database with the one from dev database
    Console.WriteLine($"Updating field with ID {field.MetaInfo!.ID} in local database...");
    await fieldLocalClient.PostFieldAsync(field);
}

foreach (var cartographicProjectionSet in cartographicProjectionSets)
{
    Console.WriteLine($"Updating cartographic projection set with ID {cartographicProjectionSet.MetaInfo!.ID} in local database...");
    // Update each cartographic projection set in local database with the one from dev database
    await cartographicLocalClient.PostCartographicConversionSetAsync(cartographicProjectionSet);
}

// Functions
Client ClientSetup(string _hostName, string _hostBase)
{
    HttpClient httpClient;
    Client api;
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
    httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(_hostName + _hostBase)
    };
    httpClient.DefaultRequestHeaders.Accept.Clear();
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    api = new Client(httpClient.BaseAddress.ToString(), httpClient);
    return api;
}
