using Colossal.Serialization.Entities;
using FindStuff.Systems;
using Unity.Entities;

namespace FindStuff.Prefabs
{
    public struct PloppableBuildingData() : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version = PloppableRICOSystem.kComponentVersion;

        public readonly void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
        }
    }
}
