


#region ComponentMaskDeserializers
// -----------------------------------
//  ComponentMaskDeserializers.cs
// -----------------------------------
			
namespace Coherence.Generated.Internal
{
	using Unity.Transforms;
	using Coherence.Replication.Unity;
	using Coherence.Replication.Protocol.Definition;
	using global::Coherence.Generated;


public class UnityReaders
{
    private CoherenceToUnityConverters coherenceToUnityConverters;

    public UnityReaders(UnityMapper mapper)
    {
        coherenceToUnityConverters = new CoherenceToUnityConverters(mapper);
    }
	
	public uint Read(ref Translation data, IInBitStream bitstream)
	{
		var propertyMask = (uint)0;


		if (bitstream.ReadMask()) 
		{
			var coherenceField = bitstream.ReadVector3f(24, 2400);
			     data.Value = coherenceToUnityConverters.ToUnityfloat3(coherenceField);
			propertyMask |= 0b00000000000000000000000000000001;
		}
       
		return propertyMask;
	}

	
	public uint Read(ref Rotation data, IInBitStream bitstream)
	{
		var propertyMask = (uint)0;


		if (bitstream.ReadMask()) 
		{
			var coherenceField = bitstream.ReadUnitRotation();
			     data.Value = coherenceToUnityConverters.ToUnityquaternion(coherenceField);
			propertyMask |= 0b00000000000000000000000000000001;
		}
       
		return propertyMask;
	}

	
	public uint Read(ref LocalUser data, IInBitStream bitstream)
	{
		var propertyMask = (uint)0;


		if (bitstream.ReadMask()) 
		{
			var coherenceField = bitstream.ReadIntegerRange(15, -9999);
			       data.localIndex = coherenceField;
			propertyMask |= 0b00000000000000000000000000000001;
		}
       
		return propertyMask;
	}

	
	public uint Read(ref WorldPositionQuery data, IInBitStream bitstream)
	{
		var propertyMask = (uint)0;


		if (bitstream.ReadMask()) 
		{
			var coherenceField = bitstream.ReadVector3f(24, 2400);
			     data.position = coherenceToUnityConverters.ToUnityfloat3(coherenceField);
			propertyMask |= 0b00000000000000000000000000000001;
		}

		if (bitstream.ReadMask()) 
		{
			var coherenceField = bitstream.ReadFixedPoint(24, 2400);
			     data.radius = coherenceToUnityConverters.ToUnityfloat(coherenceField);
			propertyMask |= 0b00000000000000000000000000000010;
		}
       
		return propertyMask;
	}

	
	public uint Read(ref ArchetypeComponent data, IInBitStream bitstream)
	{
		var propertyMask = (uint)0;


		if (bitstream.ReadMask()) 
		{
			var coherenceField = bitstream.ReadIntegerRange(15, -9999);
			       data.index = coherenceField;
			propertyMask |= 0b00000000000000000000000000000001;
		}
       
		return propertyMask;
	}

	
	public uint Read(ref Persistence data, IInBitStream bitstream)
	{
		var propertyMask = (uint)0;


		if (bitstream.ReadMask()) 
		{
			var coherenceField = bitstream.ReadShortString();
			     data.uuid = coherenceToUnityConverters.ToUnityFixedString64(coherenceField);
			propertyMask |= 0b00000000000000000000000000000001;
		}

		if (bitstream.ReadMask()) 
		{
			var coherenceField = bitstream.ReadShortString();
			     data.expiry = coherenceToUnityConverters.ToUnityFixedString64(coherenceField);
			propertyMask |= 0b00000000000000000000000000000010;
		}
       
		return propertyMask;
	}

	
	public uint Read(ref Player data, IInBitStream bitstream)
	{
		var propertyMask = (uint)0;

       
		return propertyMask;
	}

	
}

}

// ------------------ end of ComponentMaskDeserializers.cs -----------------
#endregion
