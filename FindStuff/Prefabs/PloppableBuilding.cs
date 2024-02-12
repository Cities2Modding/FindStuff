using Colossal.Serialization.Entities;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace FindStuff.Prefabs
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct PloppableBuilding : IComponentData, IQueryTypeParameter, IEmptySerializable
    {
    }
}
