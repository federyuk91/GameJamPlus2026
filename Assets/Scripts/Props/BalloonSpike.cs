using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class BalloonSpike : MonoBehaviour
    {
        [SerializeField] private Collider2D contactArea;
        private readonly List<Collider2D> contacts = new List<Collider2D>(8);

        private void FixedUpdate()
        {
            if (contactArea == null || !contactArea.enabled) return;
            // I corpi simulati muovono i Transform senza generare OnTriggerEnter2D.
            Physics2D.SyncTransforms();
            contactArea.Overlap(new ContactFilter2D { useTriggers = false }, contacts);
            foreach (Collider2D contact in contacts)
            {
                BalloonLift balloon = contact.GetComponentInParent<BalloonLift>();
                if (balloon != null) balloon.Pop();
            }
        }

        private void Awake()
        {
            if (contactArea == null) contactArea = GetComponent<Collider2D>();
            contactArea.isTrigger = true;
        }

        private void Reset()
        {
            contactArea = GetComponent<Collider2D>();
            contactArea.isTrigger = true;
        }

        private void OnValidate()
        {
            if (contactArea != null) contactArea.isTrigger = true;
        }
    }
}
