using Colossal.Serialization.Entities;
using FindStuff.Systems;
using Unity.Entities;

namespace FindStuff.Prefabs
{
    public struct PloppableBuildingData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version = PloppableRICOSystem.kComponentVersion;
        public bool historical = false;

        public PloppableBuildingData()
        {
        }

        public readonly void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(historical);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out historical);
        }
    }
}
