using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.NetworkProperties
{
    public class ExampleEffect
    {
        public GameObject Target { get; }

        public void Start()
        {
            var health = Target.GetNetworkProperty<int>("Health");
            Target.SetNetworkProperty<int>("Health", health + 10);

            // or
            Target.GetNetworkPropertyRef<int>("Health") += 10;
        }

        public void End()
        {
        }
    }

    public class ExampleAbility
    {
#if NET_STANDARD_2_1
        private Collider[] m_ColliderCache = new Collider[16];
#endif

        public void DealDamageInArea(Vector3 position, float radius)
        {
#if NET_STANDARD_2_1
                     // run physics query
            int count = Physics.OverlapSphereNonAlloc(position, radius, m_ColliderCache);
            // slice results to span
            var colliders = new Span(m_ColliderCache, count);
#else
            var colliders = Physics.OverlapSphere(position, radius);
#endif

            var energyFilter = new Filter(new []{"Mana", "Rage"});
            // filter for property
            var filteredComponents = NetworkProperties.FilterComponents(colliders, new Filter("Health"));

            foreach (var filteredComponent in filteredComponents)
            {
                filteredComponent.GetNetworkPropertyRef<int>("Health") -= 10;
            }
        }
    }
}
