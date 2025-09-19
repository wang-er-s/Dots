using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct CameraTarget : IComponentData
{
    public UnityObjectRef<Transform> CameraTrans;
}

public struct InitializeCameraTargetTag : IComponentData { }

public struct PlayerTag : IComponentData { }
    
public class PlayerAuthoring : MonoBehaviour
{
    private class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<PlayerTag>(entity);
            AddComponent<CameraTarget>(entity);
            AddComponent<InitializeCameraTargetTag>(entity);
        }
    }
}

public partial struct CameraInitializeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // 有InitializeCameraTargetTag的实体时才会更新
        state.RequireForUpdate<InitializeCameraTargetTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (CameraTargetSingleton.Instance == null) return;
        var cameraTrans = CameraTargetSingleton.Instance.transform;
        // 使用当前world的临时缓存，
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        foreach (var (cameraTarget, entity) in SystemAPI.Query<RefRW<CameraTarget>>().WithAll<PlayerTag, InitializeCameraTargetTag>().WithEntityAccess())
        {
            cameraTarget.ValueRW.CameraTrans = cameraTrans;
            ecb.RemoveComponent<InitializeCameraTargetTag>(entity);
        }
        ecb.Playback(state.EntityManager);
    }
}

// 相机跟随,在坐标转换之后
[UpdateAfter(typeof(TransformSystemGroup))]
public partial struct MoveCameraSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (cameraTarget, playerTrans) in SystemAPI.Query<RefRW<CameraTarget>, RefRO<LocalToWorld>>()
                     .WithAll<PlayerTag>().WithNone<InitializeCameraTargetTag>())
        {
            cameraTarget.ValueRO.CameraTrans.Value.position = playerTrans.ValueRO.Position;
        }
    }
}

public partial class PlayerInputSystem : SystemBase
{
    private SurvivorsInput input;

    protected override void OnCreate()
    { 
        input = new SurvivorsInput();
        input.Enable();
    }

    protected override void OnUpdate()
    {
		var moveDirection = (float2)input.Player.Move.ReadValue<Vector2>();
        foreach (var characterMoveDirection in SystemAPI.Query<RefRW<CharacterMoveDirection>>().WithAll<PlayerTag>())
        {
            characterMoveDirection.ValueRW.Value = moveDirection;
        }
    }
}
