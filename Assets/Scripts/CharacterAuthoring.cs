using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;

public struct InitializeCharacterFlag : IComponentData, IEnableableComponent { }

public struct CharacterMoveDirection : IComponentData
{
    public float2 Value;
}

public struct CharacterMoveSpeed : IComponentData
{
    public float Value;
}

[MaterialProperty("_FacingDirection")]
public struct FaceDirectionOverride : IComponentData
{
    public float Value;
}

public class CharacterAuthoring : MonoBehaviour
{
    public float MoveSpeed;
    private class Baker : Baker<CharacterAuthoring>
    {
        public override void Bake(CharacterAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);   
            AddComponent<InitializeCharacterFlag>(entity);
            AddComponent<CharacterMoveDirection>(entity);
            AddComponent(entity, new CharacterMoveSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new FaceDirectionOverride { Value = 1.0f });
        }
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct CharacterInitializeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (initialize, physicsMass) in SystemAPI.Query<EnabledRefRW<InitializeCharacterFlag>, RefRW<PhysicsMass>>())
        {
            physicsMass.ValueRW.InverseInertia = float3.zero;
            initialize.ValueRW = false;
        }
    }
}

public partial struct CharacterMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (moveDirection, moveSpeed, physicsVelocity, faceDirectionOverride) in
                 SystemAPI.Query<RefRO<CharacterMoveDirection>, RefRO<CharacterMoveSpeed>, RefRW<PhysicsVelocity>, RefRW<FaceDirectionOverride>> ())
        {
            var velocity = new float3(moveDirection.ValueRO.Value * moveSpeed.ValueRO.Value, 0);
            physicsVelocity.ValueRW.Linear = velocity;
            if (velocity.x >= 0)
            {
                faceDirectionOverride.ValueRW.Value = 1.0f;
            }else
            {
                faceDirectionOverride.ValueRW.Value = -1.0f;
            }
        }
    }
}

public partial struct ShaderGlobalTimeSystem : ISystem
{
    private static readonly int GlobalTime = Shader.PropertyToID("_GlobalTime");

    public void OnUpdate(ref SystemState state)
    {
        var time = SystemAPI.Time.ElapsedTime;
        Shader.SetGlobalFloat(GlobalTime, (float)time);
    }
}