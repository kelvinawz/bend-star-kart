using UnityEngine;

namespace BendStarKart
{
    [RequireComponent(typeof(KartController), typeof(KartBoost))]
    public sealed class KartEffects : MonoBehaviour
    {
        [SerializeField] private Transform leftRear;
        [SerializeField] private Transform rightRear;
        [SerializeField] private Material tireMaterial;
        [SerializeField] private Material sparkMaterial;
        [SerializeField] private Material flameMaterial;
        [SerializeField, Min(0f)] private float tireMarkTime = 2.5f;
        [SerializeField, Min(0f)] private float tireMarkWidth = 0.18f;
        [SerializeField, Min(0f)] private float sparkRate = 38f;
        [SerializeField, Min(0f)] private float flameRate = 58f;

        private KartController kart;
        private KartBoost boost;
        private TrailRenderer[] tireTrails;
        private ParticleSystem[] sparks;
        private ParticleSystem[] flames;
        private bool drifting;
        private bool nitro;

        private void Awake()
        {
            kart = GetComponent<KartController>();
            boost = GetComponent<KartBoost>();
            if (leftRear == null || rightRear == null) return;
            Transform[] anchors = { leftRear, rightRear };
            tireTrails = new TrailRenderer[2];
            sparks = new ParticleSystem[2];
            flames = new ParticleSystem[2];
            for (int i = 0; i < 2; i++)
            {
                tireTrails[i] = anchors[i].gameObject.AddComponent<TrailRenderer>();
                tireTrails[i].sharedMaterial = tireMaterial;
                tireTrails[i].time = tireMarkTime;
                tireTrails[i].startWidth = tireMarkWidth;
                tireTrails[i].endWidth = tireMarkWidth * 0.4f;
                tireTrails[i].minVertexDistance = 0.12f;
                tireTrails[i].emitting = false;
                sparks[i] = CreateParticles(anchors[i], "漂移火花", sparkMaterial,
                    0.2f, 0.13f, 2.8f, false);
                flames[i] = CreateParticles(anchors[i], "氮气尾焰", flameMaterial,
                    0.28f, 0.28f, 2.2f, true);
            }
        }

        private void OnEnable()
        {
            if (kart != null) kart.DriftChanged += OnDriftChanged;
            if (boost != null) boost.NitroChanged += OnNitroChanged;
        }

        private void OnDisable()
        {
            if (kart != null) kart.DriftChanged -= OnDriftChanged;
            if (boost != null) boost.NitroChanged -= OnNitroChanged;
            if (tireTrails == null) return;
            for (int i = 0; i < tireTrails.Length; i++)
                tireTrails[i].emitting = false;
        }

        private void Update()
        {
            if (tireTrails == null) return;
            bool skid = drifting && kart.IsGrounded && kart.Speed > 7f;
            for (int i = 0; i < 2; i++)
            {
                tireTrails[i].emitting = skid;
                ParticleSystem.EmissionModule sparkEmission = sparks[i].emission;
                sparkEmission.rateOverTime = skid ? sparkRate : 0f;
                ParticleSystem.EmissionModule flameEmission = flames[i].emission;
                flameEmission.rateOverTime = nitro ? flameRate : 0f;
            }
        }

        private static ParticleSystem CreateParticles(Transform anchor, string name,
            Material material, float lifetime, float size, float startSpeed,
            bool exhaust)
        {
            GameObject effect = new GameObject(name);
            effect.transform.SetParent(anchor, false);
            if (exhaust)
            {
                effect.transform.localPosition = new Vector3(0f, 0.14f, -0.22f);
                effect.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            ParticleSystem system = effect.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime;
            main.startSpeed = startSpeed;
            main.startSize = size;
            main.maxParticles = 120;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = exhaust ? ParticleSystemShapeType.Cone :
                ParticleSystemShapeType.Sphere;
            shape.radius = exhaust ? 0.07f : 0.04f;
            if (exhaust) shape.angle = 9f;
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play();
            return system;
        }

        private void OnDriftChanged(bool active) { drifting = active; }
        private void OnNitroChanged(bool active) { nitro = active; }

        public void Configure(Transform left, Transform right, Material mark,
            Material spark, Material flame)
        {
            leftRear = left;
            rightRear = right;
            tireMaterial = mark;
            sparkMaterial = spark;
            flameMaterial = flame;
        }
    }
}
