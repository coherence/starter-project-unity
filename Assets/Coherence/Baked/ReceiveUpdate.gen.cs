


#region ReceiveUpdate
// -----------------------------------
//  ReceiveUpdate.cs
// -----------------------------------
			
namespace Coherence.Generated.Internal
{
	using Coherence.Ecs;
	using Coherence.DeltaEcs;
	using Coherence.Replication.Unity;
	using Coherence.Replication.Client.Unity.Ecs;
	using global::Unity.Transforms;
	using global::Unity.Collections;
	using global::Unity.Entities;
	using Coherence.Brook;
	using Coherence.Log;
	using Coherence.SimulationFrame;
	using global::Coherence.Generated;

	public class ReceiveUpdate : IReceiveUpdate
	{
		private readonly ISchemaSpecificComponentDeserialize componentDeserialize;
		private UnityMapper mapper;
		private readonly ISchemaSpecificComponentDeserializerAndSkip componentSkip;
		private NativeHashMap<Entity, DetectedEntityDeletion> destroyedEntities;

		public ReceiveUpdate(ISchemaSpecificComponentDeserialize componentDeserialize,  ISchemaSpecificComponentDeserializerAndSkip componentSkip, UnityMapper mapper, NativeHashMap<Entity, DetectedEntityDeletion> destroyedEntities)
		{
			this.componentDeserialize = componentDeserialize;
			this.componentSkip = componentSkip;
			this.mapper = mapper;
			this.destroyedEntities = destroyedEntities;
		}

		private void DestroyComponentData(EntityManager entityManager, Entity entity, uint componentType)
		{
			switch (componentType)
			{

				case TypeIds.InternalWorldPosition:
				{
					var hasComponentData = entityManager.HasComponent<Translation>(entity);
					if (hasComponentData)
					{
						entityManager.RemoveComponent<Translation>(entity);
					}
					break;
				}

				case TypeIds.InternalWorldOrientation:
				{
					var hasComponentData = entityManager.HasComponent<Rotation>(entity);
					if (hasComponentData)
					{
						entityManager.RemoveComponent<Rotation>(entity);
					}
					break;
				}

				case TypeIds.InternalLocalUser:
				{
					var hasComponentData = entityManager.HasComponent<LocalUser>(entity);
					if (hasComponentData)
					{
						entityManager.RemoveComponent<LocalUser>(entity);
					}
					break;
				}

				case TypeIds.InternalWorldPositionQuery:
				{
					var hasComponentData = entityManager.HasComponent<WorldPositionQuery>(entity);
					if (hasComponentData)
					{
						entityManager.RemoveComponent<WorldPositionQuery>(entity);
					}
					break;
				}

				case TypeIds.InternalArchetypeComponent:
				{
					var hasComponentData = entityManager.HasComponent<ArchetypeComponent>(entity);
					if (hasComponentData)
					{
						entityManager.RemoveComponent<ArchetypeComponent>(entity);
					}
					break;
				}

				case TypeIds.InternalPersistence:
				{
					var hasComponentData = entityManager.HasComponent<Persistence>(entity);
					if (hasComponentData)
					{
						entityManager.RemoveComponent<Persistence>(entity);
					}
					break;
				}

				case TypeIds.InternalPlayer:
				{
					var hasComponentData = entityManager.HasComponent<Player>(entity);
					if (hasComponentData)
					{
						entityManager.RemoveComponent<Player>(entity);
					}
					break;
				}

				default:
				{
					Log.Warning($"Unknown component", "component", componentType);
					break;
				}
			}
		}

		private void UpdateComponents(EntityManager entityManager, Entity entity, AbsoluteSimulationFrame simulationFrame, IInBitStream bitStream)
		{
			var componentCount = Deserializator.ReadComponentCount(bitStream);
			for (var i = 0; i < componentCount; i++)
			{
				var componentState = Deserializator.ReadComponentState(bitStream);
				var componentId = Deserializator.ReadComponentId(bitStream);
				switch (componentState)
				{
					case ComponentState.Construct:
						{
							var componentTypeId = Deserializator.ReadComponentTypeId(bitStream);

							componentDeserialize.CreateIfNeededAndReadComponentDataUpdate(entityManager,
								entity, componentTypeId, simulationFrame, bitStream);
						}
						break;
					case ComponentState.Update:
						{
							// TODO: lookup component ID from state.
							var updateComponentTypeId = componentId;
							componentDeserialize.ReadComponentDataUpdate(entityManager, entity,
								updateComponentTypeId, simulationFrame, bitStream);
						}
						break;
					case ComponentState.Destruct:
						{
							var destroyComponentTypeId = componentId;
							DestroyComponentData(entityManager, entity, destroyComponentTypeId);
						}
						break;
				}
			}
		}

		public void PerformUpdate(EntityManager entityManager, AbsoluteSimulationFrame simulationFrame, IInBitStream bitStream)
		{
			var deserializeEntity = new Deserializator();

			while (deserializeEntity.ReadEntity(bitStream, out var entityWithMeta))
			{
				var entity = mapper.ToUnityEntity(entityWithMeta.EntityId);

				var wasSimulated = entity != default && entityManager.Exists(entity) && entityManager.HasComponent<Simulated>(entity);

				// Skip locally destroyed entities
				if (destroyedEntities.ContainsKey(entity))
				{
					if (!entityWithMeta.IsDeleted)
					{
						DeserializeComponentSkip.SkipComponents(componentSkip, bitStream);
					}
					continue;
				}

				// Meta information concerns entity creation, destruction and ownership
				if (entityWithMeta.HasMeta)
				{
					entity = PerformEntityMetaUpdate(entityManager, entityWithMeta, entity);
				}

				// Deserialize and apply component updates
				if (entity != default)
				{
					var isSimulated = entityManager.HasComponent<Simulated>(entity);
					var wasTransferred = isSimulated && !wasSimulated;
					// Only update components for non-simulated entities - unless it was transferred with this packet 
					if (!isSimulated || wasTransferred)
					{
						UpdateComponents(entityManager, entity, simulationFrame, bitStream);
					}
					else
					{
						DeserializeComponentSkip.SkipComponents(componentSkip, bitStream);
						Log.Warning($"Trying to update owned entity {entityWithMeta.EntityId}");
					}
				} else if (!entityWithMeta.IsDeleted)
				{
					// An error has occurred if the entity is null unless it's because it was just deleted
					Log.Warning($"Entity is missing {entityWithMeta.EntityId}");

					DeserializeComponentSkip.SkipComponents(componentSkip, bitStream);
				}
			}
		}

		private Entity PerformEntityMetaUpdate(EntityManager entityManager, Deserializator.EntityWithMeta entityWithMeta, Entity entity)
		{
			// Log a warning and remove mapping if an entity has been destroyed locally
			if (entity != default && !entityManager.Exists(entity))
			{
				UnityEngine.Debug.LogWarning($"{entity} does not exist, did you destroy a non-simulated entity?");
				entity = default;
				mapper.Remove(entityWithMeta.EntityId);
			}

			// Entities are CREATED implicitly if they do not exist
			if (entity == default)
			{
				entity = entityManager.CreateEntity();
				mapper.Add(entityWithMeta.EntityId, entity);
				entityManager.AddComponent<Mapped>(entity);
			}

			// Entities OWNERSHIP determines iff they should have Simulated
			var hasComponentData = entityManager.HasComponent<Simulated>(entity);
			if (hasComponentData && !entityWithMeta.Ownership)
			{
				entityManager.RemoveComponent<Simulated>(entity);
				entityManager.RemoveComponent<LingerSimulated>(entity);
				RemoveSyncComponents(entityManager, entity);
				AddCommandBuffers(entityManager, entity);
			}
			else if (!hasComponentData && entityWithMeta.Ownership)
			{
				entityManager.AddComponentData(entity, new Simulated());
				RemoveInterpolationComponents(entityManager, entity);
			}

			// Entities IsOrphan determines iff they should have Orphan
			var hasOrphanComponent = entityManager.HasComponent<Orphan>(entity);
			if (hasOrphanComponent && !entityWithMeta.IsOrphan)
			{
				entityManager.RemoveComponent<Orphan>(entity);
			}
			else if (!hasOrphanComponent && entityWithMeta.IsOrphan)
			{
				entityManager.AddComponentData(entity, new Orphan());
			}

			// Entities are DELETED explicitly by the IsDeleted flag
			if (entityWithMeta.IsDeleted)
			{
				if (!entityWithMeta.Ownership)
				{
					Log.Debug($"Deleting entity {entityWithMeta.Ownership} {entityWithMeta.EntityId}");
					if (entity != default)
					{
						if (entityManager.Exists(entity))
						{
							mapper.Remove(entityWithMeta.EntityId); // This internally requires entity to exist...
							entityManager.RemoveComponent<LingerSimulated>(entity);
							entityManager.DestroyEntity(entity);    // ...so this must be executed afterwards ...
						}
						else
						{
							Log.Warning($"Entity has already been deleted: {entityWithMeta.EntityId} : {entity}");
						}
					}
					else
					{
						Log.Warning($"Attempted to delete missing entity: {entityWithMeta.EntityId}");
					}
				}
				else
				{
					Log.Warning($"Attempted to delete owned entity: {entityWithMeta.EntityId}");
				}

				return default;
			}

			return entity;
		}

		public void UpdateResendMask(EntityManager entityManager, Coherence.Ecs.SerializeEntityID entityId, uint componentTypeId, uint fieldMask)
		{
			var entity = mapper.ToUnityEntity(entityId);

			if (!entityManager.Exists(entity))
			{
				Log.Warning($"Entity does not exist: {entity} ComponentTypeId: {componentTypeId}");
				return;
			}

			switch (componentTypeId)
			{

				case TypeIds.InternalWorldPosition:
				{
					var hasComponentData = entityManager.HasComponent<WorldPosition_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<WorldPosition_Sync>(entity);

						syncData.resendMask |= fieldMask;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						Log.Warning($"Entity or component has been destroyed: {entity} ComponentTypeId: {componentTypeId}");
					}
					break;
				}

				case TypeIds.InternalWorldOrientation:
				{
					var hasComponentData = entityManager.HasComponent<WorldOrientation_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<WorldOrientation_Sync>(entity);

						syncData.resendMask |= fieldMask;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						Log.Warning($"Entity or component has been destroyed: {entity} ComponentTypeId: {componentTypeId}");
					}
					break;
				}

				case TypeIds.InternalLocalUser:
				{
					var hasComponentData = entityManager.HasComponent<LocalUser_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<LocalUser_Sync>(entity);

						syncData.resendMask |= fieldMask;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						Log.Warning($"Entity or component has been destroyed: {entity} ComponentTypeId: {componentTypeId}");
					}
					break;
				}

				case TypeIds.InternalWorldPositionQuery:
				{
					var hasComponentData = entityManager.HasComponent<WorldPositionQuery_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<WorldPositionQuery_Sync>(entity);

						syncData.resendMask |= fieldMask;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						Log.Warning($"Entity or component has been destroyed: {entity} ComponentTypeId: {componentTypeId}");
					}
					break;
				}

				case TypeIds.InternalArchetypeComponent:
				{
					var hasComponentData = entityManager.HasComponent<ArchetypeComponent_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<ArchetypeComponent_Sync>(entity);

						syncData.resendMask |= fieldMask;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						Log.Warning($"Entity or component has been destroyed: {entity} ComponentTypeId: {componentTypeId}");
					}
					break;
				}

				case TypeIds.InternalPersistence:
				{
					var hasComponentData = entityManager.HasComponent<Persistence_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<Persistence_Sync>(entity);

						syncData.resendMask |= fieldMask;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						Log.Warning($"Entity or component has been destroyed: {entity} ComponentTypeId: {componentTypeId}");
					}
					break;
				}

				case TypeIds.InternalPlayer:
				{
					var hasComponentData = entityManager.HasComponent<Player_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<Player_Sync>(entity);

						syncData.resendMask |= fieldMask;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						Log.Warning($"Entity or component has been destroyed: {entity} ComponentTypeId: {componentTypeId}");
					}
					break;
				}

				default:
				{
					Log.Warning($"Unknown component", "component", componentTypeId);
					break;
				}
			}
		}

		public void UpdateHasReceivedConstructor(EntityManager entityManager, Coherence.Ecs.SerializeEntityID entityId, uint componentTypeId)
		{
			var entity = mapper.ToUnityEntity(entityId);

			// The entity has been deleted since the packet was sent
			if (destroyedEntities.ContainsKey(entity))
			{
				return;
			}

			if (!entityManager.Exists(entity))
			{
				Log.Warning($"Entity does not exist: {entity} ComponentTypeId: {componentTypeId}");
				return;
			}

			if (!entityManager.HasComponent<Simulated>(entity))
			{
				// Ownership may have been lost since the packet was sent
				Log.Trace($"Entity is missing Simulated: {entity} ComponentTypeId: {componentTypeId}");
				return;
			}

			var sim = entityManager.GetComponentData<Simulated>(entity);
			sim.hasReceivedConstructor = true;

			switch (componentTypeId)
			{

				case TypeIds.InternalWorldPosition:
				{
					var hasComponentData = entityManager.HasComponent<WorldPosition_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<WorldPosition_Sync>(entity);
						syncData.hasReceivedConstructor = true;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						// Ownership may have been lost since the packet was sent
						Log.Trace($"Sync component has been destroyed: {entity} WorldPosition_Sync");
					}
					break;
				}

				case TypeIds.InternalWorldOrientation:
				{
					var hasComponentData = entityManager.HasComponent<WorldOrientation_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<WorldOrientation_Sync>(entity);
						syncData.hasReceivedConstructor = true;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						// Ownership may have been lost since the packet was sent
						Log.Trace($"Sync component has been destroyed: {entity} WorldOrientation_Sync");
					}
					break;
				}

				case TypeIds.InternalLocalUser:
				{
					var hasComponentData = entityManager.HasComponent<LocalUser_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<LocalUser_Sync>(entity);
						syncData.hasReceivedConstructor = true;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						// Ownership may have been lost since the packet was sent
						Log.Trace($"Sync component has been destroyed: {entity} LocalUser_Sync");
					}
					break;
				}

				case TypeIds.InternalWorldPositionQuery:
				{
					var hasComponentData = entityManager.HasComponent<WorldPositionQuery_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<WorldPositionQuery_Sync>(entity);
						syncData.hasReceivedConstructor = true;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						// Ownership may have been lost since the packet was sent
						Log.Trace($"Sync component has been destroyed: {entity} WorldPositionQuery_Sync");
					}
					break;
				}

				case TypeIds.InternalArchetypeComponent:
				{
					var hasComponentData = entityManager.HasComponent<ArchetypeComponent_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<ArchetypeComponent_Sync>(entity);
						syncData.hasReceivedConstructor = true;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						// Ownership may have been lost since the packet was sent
						Log.Trace($"Sync component has been destroyed: {entity} ArchetypeComponent_Sync");
					}
					break;
				}

				case TypeIds.InternalPersistence:
				{
					var hasComponentData = entityManager.HasComponent<Persistence_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<Persistence_Sync>(entity);
						syncData.hasReceivedConstructor = true;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						// Ownership may have been lost since the packet was sent
						Log.Trace($"Sync component has been destroyed: {entity} Persistence_Sync");
					}
					break;
				}

				case TypeIds.InternalPlayer:
				{
					var hasComponentData = entityManager.HasComponent<Player_Sync>(entity);
					if (hasComponentData)
					{
						var syncData = entityManager.GetComponentData<Player_Sync>(entity);
						syncData.hasReceivedConstructor = true;
						entityManager.SetComponentData(entity, syncData);
					} else
					{
						// Ownership may have been lost since the packet was sent
						Log.Trace($"Sync component has been destroyed: {entity} Player_Sync");
					}
					break;
				}

				default:
				{
					Log.Warning($"Unknown component", "component", componentTypeId);
					break;
				}
			}
		}

		public void UpdateResendDestroyed(EntityManager entityManager, Coherence.Ecs.SerializeEntityID entityId, AbsoluteSimulationFrame simulationFrame)
		{
			var entity = mapper.ToUnityEntity(entityId);
			if (entity == default)
			{
				Log.Warning($"Destroyed entity {entityId} missing from mapper");
				return;
			}

			// Flag this entity destruction to be resent
			destroyedEntities[entity] = new DetectedEntityDeletion { Entity = entity, simulationFrame = (ulong)simulationFrame.Frame, serialized = false };
		}

		public static void RemoveSyncComponents(EntityManager entityManager, Entity entity)
		{

			if (entityManager.HasComponent<WorldPosition_Sync>(entity))
			{
				entityManager.RemoveComponent<WorldPosition_Sync>(entity);
			}

			if (entityManager.HasComponent<WorldOrientation_Sync>(entity))
			{
				entityManager.RemoveComponent<WorldOrientation_Sync>(entity);
			}

			if (entityManager.HasComponent<LocalUser_Sync>(entity))
			{
				entityManager.RemoveComponent<LocalUser_Sync>(entity);
			}

			if (entityManager.HasComponent<WorldPositionQuery_Sync>(entity))
			{
				entityManager.RemoveComponent<WorldPositionQuery_Sync>(entity);
			}

			if (entityManager.HasComponent<ArchetypeComponent_Sync>(entity))
			{
				entityManager.RemoveComponent<ArchetypeComponent_Sync>(entity);
			}

			if (entityManager.HasComponent<Persistence_Sync>(entity))
			{
				entityManager.RemoveComponent<Persistence_Sync>(entity);
			}

			if (entityManager.HasComponent<Player_Sync>(entity))
			{
				entityManager.RemoveComponent<Player_Sync>(entity);
			}

		}

		public static void AddCommandBuffers(EntityManager entityManager, Entity entity)
		{
#region Commands

			{
				var hasBuffer = entityManager.HasComponent<AuthorityTransfer>(entity);
				if (!hasBuffer)
				{
					entityManager.AddBuffer<AuthorityTransfer>(entity);
				}

				var hasRequestBuffer = entityManager.HasComponent<AuthorityTransferRequest>(entity);
				if (!hasRequestBuffer)
				{
					entityManager.AddBuffer<AuthorityTransferRequest>(entity);
				}
			}

#endregion
		}

		private void RemoveInterpolationComponents(EntityManager entityManager, Entity entity)
		{


			if (entityManager.HasComponent<InterpolationComponent_Translation>(entity))
			{
				entityManager.RemoveComponent<InterpolationComponent_Translation>(entity);
			}
			if (entityManager.HasComponent<Sample_Translation>(entity))
			{
				entityManager.RemoveComponent<Sample_Translation>(entity);
			}



			if (entityManager.HasComponent<InterpolationComponent_Rotation>(entity))
			{
				entityManager.RemoveComponent<InterpolationComponent_Rotation>(entity);
			}
			if (entityManager.HasComponent<Sample_Rotation>(entity))
			{
				entityManager.RemoveComponent<Sample_Rotation>(entity);
			}












		}
	}
}
// ------------------ end of ReceiveUpdate.cs -----------------
#endregion
