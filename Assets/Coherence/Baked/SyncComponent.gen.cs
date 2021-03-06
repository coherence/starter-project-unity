


#region SyncComponent
// -----------------------------------
//  SyncComponent.cs
// -----------------------------------
			
namespace Coherence.Generated.Internal
{
    using Unity.Entities;
    using Unity.Transforms;
    using global::Coherence.Generated;


    public struct WorldPosition_Sync : IComponentData
    {
        public Translation lastSentData;
        public uint resendMask;
        public uint howImportantAreYou;
        public uint accumulatedPriority;
        public long deletedAtTime;
        public bool hasBeenSerialized;
        public bool deleteHasBeenSerialized;
        public bool hasReceivedConstructor;
    }


    public struct WorldOrientation_Sync : IComponentData
    {
        public Rotation lastSentData;
        public uint resendMask;
        public uint howImportantAreYou;
        public uint accumulatedPriority;
        public long deletedAtTime;
        public bool hasBeenSerialized;
        public bool deleteHasBeenSerialized;
        public bool hasReceivedConstructor;
    }


    public struct LocalUser_Sync : IComponentData
    {
        public LocalUser lastSentData;
        public uint resendMask;
        public uint howImportantAreYou;
        public uint accumulatedPriority;
        public long deletedAtTime;
        public bool hasBeenSerialized;
        public bool deleteHasBeenSerialized;
        public bool hasReceivedConstructor;
    }


    public struct WorldPositionQuery_Sync : IComponentData
    {
        public WorldPositionQuery lastSentData;
        public uint resendMask;
        public uint howImportantAreYou;
        public uint accumulatedPriority;
        public long deletedAtTime;
        public bool hasBeenSerialized;
        public bool deleteHasBeenSerialized;
        public bool hasReceivedConstructor;
    }


    public struct ArchetypeComponent_Sync : IComponentData
    {
        public ArchetypeComponent lastSentData;
        public uint resendMask;
        public uint howImportantAreYou;
        public uint accumulatedPriority;
        public long deletedAtTime;
        public bool hasBeenSerialized;
        public bool deleteHasBeenSerialized;
        public bool hasReceivedConstructor;
    }


    public struct Persistence_Sync : IComponentData
    {
        public Persistence lastSentData;
        public uint resendMask;
        public uint howImportantAreYou;
        public uint accumulatedPriority;
        public long deletedAtTime;
        public bool hasBeenSerialized;
        public bool deleteHasBeenSerialized;
        public bool hasReceivedConstructor;
    }


    public struct Player_Sync : IComponentData
    {
        public Player lastSentData;
        public uint resendMask;
        public uint howImportantAreYou;
        public uint accumulatedPriority;
        public long deletedAtTime;
        public bool hasBeenSerialized;
        public bool deleteHasBeenSerialized;
        public bool hasReceivedConstructor;
    }


}


// ------------------ end of SyncComponent.cs -----------------
#endregion
