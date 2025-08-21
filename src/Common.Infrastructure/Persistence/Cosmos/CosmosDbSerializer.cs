using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Common.Infrastructure.Persistence.Cosmos;

public class CosmosDbSerializer : CosmosSerializer
{
    private readonly JsonSerializer serializer;

    public CosmosDbSerializer()
    {
        serializer = new JsonSerializer()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
        };
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)(stream);
            }

            using StreamReader sr = new(stream);
            using JsonTextReader jsonTextReader = new(sr);

            return serializer.Deserialize<T>(jsonTextReader)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream streamPayload = new();
        using (StreamWriter streamWriter = new(streamPayload, encoding: Encoding.Default, bufferSize: 1024, leaveOpen: true))
        {
            using JsonWriter writer = new JsonTextWriter(streamWriter);

            writer.Formatting = Formatting.None;
            serializer.Serialize(writer, input);
            writer.Flush();
            streamWriter.Flush();
        }

        streamPayload.Position = 0;
        return streamPayload;
    }
}
