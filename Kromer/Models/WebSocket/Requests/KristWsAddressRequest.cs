using Newtonsoft.Json;

namespace Kromer.Models.WebSocket.Requests;

public class KristWsAddressRequest : KristWsRequest
{
    public string Address { get; set; }

    // Who decided that mixing naming cases is a good idea?
    [JsonProperty("fetchNames")]
    public bool FetchNames { get; set; }
}