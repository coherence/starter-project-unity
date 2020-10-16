using Coherence;
using Coherence.Generated.FirstProject;
using Coherence.Replication.Client.Unity.Ecs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class InputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var vec = new float2();

        if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) { vec.x -= 1; }
        if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) { vec.x += 1; }
        if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) { vec.y += 1; }
        if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) { vec.y -= 1; }

        if (math.length(vec) > 0f)
        {
            vec = math.mul(float2x2.Rotate(math.PI * 0.25f), math.normalize(vec));
        }

        Entities.ForEach((ref Input input) =>
        {
            input.Value = vec;
        }).ScheduleParallel();

        // Sending commands
        if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Sending long text");
            Entities
                .WithNone<CoherenceSimulateComponent>()
                .ForEach((Entity entity,
                          in Player player) =>
            {
                var doitRequestBuffer = EntityManager.GetBuffer<DoitRequest>(entity);
                doitRequestBuffer.Add(new DoitRequest {
                        number = counter++,
                        text = "Here's " + counter.ToString() + "!"
                    });
            }).WithStructuralChanges().WithoutBurst().Run();
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Sending short text");
            Entities
                .WithNone<CoherenceSimulateComponent>()
                .ForEach((Entity entity,
                          in Player player) =>
            {
                var doitRequestBuffer = EntityManager.GetBuffer<DoitRequest>(entity);
                doitRequestBuffer.Add(new DoitRequest {
                        number = counter++,
                        text = "x"
                    });
            }).WithStructuralChanges().WithoutBurst().Run();
        }
    }

    static int counter = 0;
}
