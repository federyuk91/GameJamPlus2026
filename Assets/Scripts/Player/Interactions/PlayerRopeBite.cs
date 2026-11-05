using System;
using UnityEngine;

namespace MousePlatformer
{
    /// <summary>
    /// Gestisce il morso prolungato che taglia una corda raggiungibile, anche in presenza di ostacoli.
    /// Ogni pressione prolungata della bocca può completare al massimo un taglio.
    /// Lo scavo usa le mani ed è gestito separatamente da PlayerDigging.
    /// </summary>
    // Viene eseguito prima del movimento del topo, che usa ordine 100.
    [DefaultExecutionOrder(90), DisallowMultipleComponent, RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerRopeBite : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private MouseFacing facing;
        [SerializeField] private Transform visual;
        [SerializeField] private PlayerRopeInteraction ropeInteraction;
        [SerializeField] private Vector2 mouthOffset = new Vector2(0.45f, -0.1f);
        [SerializeField, Min(0.01f), Tooltip("Maximum mouth-to-rope distance. Yellow rope gizmos show this reach; while hanging, the grip point is used.")]
        private float reach = 0.4f;
        [SerializeField, Min(0.01f)] private float holdDuration = 0.6f;
        [SerializeField, Tooltip("Shared input diagnostics during gameplay.")]
        private string lastAttempt;
        [SerializeField, Range(0f, 1f)] private float biteProgress;
        // Il bersaglio identifica la corda; il segmento viene aggiornato mentre questa si muove.
        private RopeSimulation target;
        private int targetSegment = -1;
        private float heldTime;
        // Dopo un taglio occorre rilasciare il comando prima di poterne eseguire un altro.
        private bool consumedHold;
        /// <summary>Avanzamento normalizzato del morso corrente, azzerato alla perdita del bersaglio.</summary>
        public float BiteProgress => biteProgress;
        public float Reach => reach;
        /// <summary>Notifica un taglio riuscito, dopo il consumo della pressione e l'azzeramento del morso.</summary>
        public event Action RopeCut;
        private void Awake()
        {
            // Le reference dell'Inspector hanno precedenza; si recuperano una sola volta
            // i componenti locali mancanti per supportare anche configurazioni precedenti.
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (facing == null) facing = GetComponent<MouseFacing>();
            if (ropeInteraction == null) ropeInteraction = GetComponent<PlayerRopeInteraction>();
        }

        private void FixedUpdate()
        {
            // Il rilascio, oppure un blocco dell'input, riabilita una nuova pressione.
            // Si usa il comando grezzo per distinguere il rilascio dall'arbitraggio delle interazioni.
            if (!input.RawBiteHeld)
            {
                consumedHold = false;
                ResetTarget();
                lastAttempt = "Waiting for F / North";
                return;
            }
            if (consumedHold) return;
            // Il comando può essere premuto ma consumato da un'altra interazione,
            // oppure non assegnato alle azioni disponibili in questo momento.
            if (!input.BiteHeld)
            {
                ResetTarget();
                return;
            }
            // Sincronizza i collider spostati via Transform prima della verifica del sostegno.
            Physics2D.SyncTransforms();
            RopeSimulation nearest = null;
            int segment = -1;
            if (input.IsInteractionSelected(AvailableGameplayActions.CutRope))
                TryFindCutTarget(out nearest, out segment);
            if (nearest == null || !nearest.CanBeCut)
            {
                // Perdere la corda annulla il tempo accumulato per il morso.
                ResetTarget();
                lastAttempt = "No cuttable rope in front of the mouth";
                return;
            }
            if (target != nearest)
            {
                // Cambiare corda richiede un nuovo morso completo; cambiare segmento
                // della stessa corda, invece, non interrompe l'avanzamento.
                target = nearest;
                heldTime = 0f;
            }
            targetSegment = segment;
            heldTime += Time.fixedDeltaTime;
            biteProgress = Mathf.Clamp01(heldTime / Mathf.Max(0.01f, holdDuration));
            lastAttempt = "Biting in progress";
            if (heldTime >= holdDuration && target.TryCutSegment(targetSegment))
            {
                // Si consuma la pressione solo dopo il taglio effettivo. Se il segmento
                // non è tagliabile, la ricerca può riprovare nei tick successivi.
                consumedHold = true;
                ResetTarget();
                lastAttempt = "Rope cut: release F / North";
                RopeCut?.Invoke();
            }
        }

        /// <summary>
        /// Verifica la disponibilità geometrica del taglio senza richiedere il tasto premuto
        /// e senza controllare gli ostacoli. Non avanza il morso né taglia la corda.
        /// </summary>
        public bool CanCutRope => isActiveAndEnabled && body != null && !consumedHold
            && TryFindCutTarget(out _, out _);

        /// <summary>
        /// Privilegia la corda impugnata; in alternativa cerca il segmento integro più vicino
        /// davanti al topo ed entro la portata della bocca, ignorando gli ostacoli solidi.
        /// </summary>
        private bool TryFindCutTarget(out RopeSimulation nearest, out int segment)
        {
            nearest = null;
            segment = -1;
            if (body == null) return false;
            // Il taglio è consentito da terra, appesi a una corda oppure immersi in acqua;
            // non durante una normale caduta o un salto senza sostegno.
            bool supported = body.Grounded || (ropeInteraction != null && ropeInteraction.Attached);
            if (!supported && !WaterVolume.ContainsPoint(body.Shape.bounds.center)) return false;
            Transform plane = visual != null ? visual : transform;
            // Offset e direzione seguono il riferimento visivo, compreso il verso del topo.
            float sign = facing == null || facing.FacingRight ? 1f : -1f;
            Vector2 mouth = plane.TransformPoint(new Vector2(mouthOffset.x * sign, mouthOffset.y));
            Vector2 forward = plane.TransformDirection(Vector2.right * sign);
            Vector2 facingOrigin = body != null ? body.transform.TransformPoint(body.Shape.offset) : transform.position;
            float best = reach * reach;
            // La corda impugnata può essere portata alla bocca indipendentemente dal verso.
            // Questo ramo usa il punto di presa, senza il limite di distanza
            // applicato più sotto alle corde libere.
            RopeSimulation heldRope = null;
            if (ropeInteraction != null
                && ropeInteraction.TryGetGrip(out heldRope, out int gripSegment, out _)
                && heldRope.CanBeCut)
            {
                // Sceglie il segmento in base al lato che deve restare utilizzabile dopo
                // il taglio, lasciando una presa sul tratto sostenuto della corda.
                int cutSegment = heldRope.KeepCrateSideAfterCut
                    ? Mathf.Min(gripSegment, heldRope.ClimbableEndSegment - 2)
                    : Mathf.Max(heldRope.ClimbableFirstSegment + 1, gripSegment);
                if (cutSegment >= heldRope.ClimbableFirstSegment && cutSegment < heldRope.ClimbableEndSegment)
                {
                    nearest = heldRope;
                    segment = cutSegment;
                }
            }
            if (nearest == null)
            {
                for (int ropeIndex = 0; ropeIndex < RopeSimulation.ActiveRopes.Count; ropeIndex++)
                {
                    RopeSimulation rope = RopeSimulation.ActiveRopes[ropeIndex];
                    if (rope == heldRope) continue;
                    if (rope == null || !rope.isActiveAndEnabled || !rope.CanBeCut || rope.Chain == null) continue;
                    for (int i = 0; i < rope.Chain.SegmentCount; i++)
                    {
                        if (!rope.Chain.IsSegmentIntact(i)) continue;
                        Vector2 a = rope.Chain[i];
                        Vector2 delta = rope.Chain[i + 1] - a;
                        // Proietta la bocca sul segmento e limita la proiezione ai suoi estremi.
                        // Il denominatore minimo evita divisioni per zero su segmenti degeneri.
                        float t = Mathf.Clamp01(Vector2.Dot(mouth - a, delta) / Mathf.Max(delta.sqrMagnitude, 0.000001f));
                        Vector2 point = a + delta * t;
                        Vector2 offset = point - mouth;
                        float squared = offset.sqrMagnitude;
                        // Il controllo "davanti/dietro" parte dal corpo, non dalla bocca:
                        // permette di mordere una corda appena oltrepassata dal solo muso.
                        // La tolleranza di 0,05 unità evita esclusioni al limite dell'allineamento.
                        bool behindPlayer = Vector2.Dot(point - facingOrigin, forward) < -0.05f;
                        if (squared > best || behindPlayer) continue;
                        best = squared;
                        nearest = rope;
                        segment = i;
                    }
                }
            }
            return nearest != null && nearest.CanBeCut;
        }

        // Azzera solo il bersaglio e l'avanzamento: conserva
        // l'eventuale pressione consumata fino al rilascio o a Cancel.
        private void ResetTarget()
        {
            target = null;
            targetSegment = -1;
            heldTime = 0f;
            biteProgress = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            // Visualizza la portata ordinaria dalla bocca e il verso di ricerca.
            // La corda già impugnata usa invece il ramo dedicato al punto di presa.
            Transform plane = visual != null ? visual : transform;
            float sign = facing == null || facing.FacingRight ? 1f : -1f;
            Vector2 mouth = plane.TransformPoint(new Vector2(mouthOffset.x * sign, mouthOffset.y));
            Vector2 forward = plane.TransformDirection(Vector2.right * sign);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(mouth, reach);
            Gizmos.DrawLine(mouth, mouth + forward * reach);
        }

        private void OnDisable()
        {
            Cancel();
        }

        /// <summary>
        /// Interrompe il morso, liberando anche lo stato della pressione.
        /// Viene richiamato alla disattivazione e può essere usato dai sistemi esterni.
        /// </summary>
        public void Cancel()
        {
            consumedHold = false;
            ResetTarget();
        }
    }
}
