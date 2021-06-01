using UnityEngine.Assertions;
using UnityEngine;
using System.Collections.Generic;

namespace BossRoom.Client
{
    /// <summary>
    /// Responsible for storing of all of the pieces of a character, and swapping the pieces out en masse when asked to.
    /// </summary>
    public class CharacterSwap : MonoBehaviour
    {
        [System.Serializable]
        public class CharacterModelSet
        {
            public GameObject ears;
            public GameObject head;
            public GameObject mouth;
            public GameObject hair;
            public GameObject eyes;
            public GameObject torso;
            public GameObject gearRightHand;
            public GameObject gearLeftHand;
            public GameObject handRight;
            public GameObject handLeft;
            public GameObject shoulderRight;
            public GameObject shoulderLeft;
            public Visual.AnimatorTriggeredSpecialFX specialFx; // should be a component on the same GameObject as the Animator!
            public AnimatorOverrideController animatorOverrides; // references a separate stand-alone object in the project
            private List<Renderer> m_CachedRenderers;

            public void SetFullActive(bool isActive)
            {
                ears.SetActive(isActive);
                head.SetActive(isActive);
                mouth.SetActive(isActive);
                hair.SetActive(isActive);
                eyes.SetActive(isActive);
                torso.SetActive(isActive);
                gearLeftHand.SetActive(isActive);
                gearRightHand.SetActive(isActive);
                handRight.SetActive(isActive);
                handLeft.SetActive(isActive);
                shoulderRight.SetActive(isActive);
                shoulderLeft.SetActive(isActive);
            }

            public List<Renderer> GetAllBodyParts()
            {
                if (m_CachedRenderers == null)
                {
                    m_CachedRenderers = new List<Renderer>();
                    AddRenderer(ref m_CachedRenderers, ears);
                    AddRenderer(ref m_CachedRenderers, head);
                    AddRenderer(ref m_CachedRenderers, mouth);
                    AddRenderer(ref m_CachedRenderers, hair);
                    AddRenderer(ref m_CachedRenderers, torso);
                    AddRenderer(ref m_CachedRenderers, gearRightHand);
                    AddRenderer(ref m_CachedRenderers, gearLeftHand);
                    AddRenderer(ref m_CachedRenderers, handRight);
                    AddRenderer(ref m_CachedRenderers, handLeft);
                    AddRenderer(ref m_CachedRenderers, shoulderRight);
                    AddRenderer(ref m_CachedRenderers, shoulderLeft);
                }
                return m_CachedRenderers;
            }

            private void AddRenderer(ref List<Renderer> rendererList, GameObject bodypartGO)
            {
                if (!bodypartGO) { return; }
                var bodyPartRenderer = bodypartGO.GetComponent<Renderer>();
                if (!bodyPartRenderer) { return; }
                rendererList.Add(bodyPartRenderer);
            }

        }

        [SerializeField]
        private CharacterModelSet[] m_CharacterModels;

        /// <summary>
        /// Reference to our shared-characters' animator.
        /// Can be null, but if so, animator overrides are not supported!
        /// </summary>
        [SerializeField]
        private Animator m_Animator;

        /// <summary>
        /// Reference to the original controller in our Animator.
        /// We switch back to this whenever we don't have an Override.
        /// </summary>
        private RuntimeAnimatorController m_OriginalController;

        [SerializeField]
        [Tooltip("Special Material we plug in when the local player is \"stealthy\"")]
        private Material m_StealthySelfMaterial;

        [SerializeField]
        [Tooltip("Special Material we plug in when another player is \"stealthy\"")]
        private Material m_StealthyOtherMaterial;

        public enum SpecialMaterialMode
        {
            None,
            StealthySelf,
            StealthyOther,
        }

        /// <summary>
        /// When we swap all our Materials out for a special material,
        /// we keep the old references here, so we can swap them back.
        /// </summary>
        private Dictionary<Renderer, Material> m_OriginalMaterials = new Dictionary<Renderer, Material>();

        private void Awake()
        {
            if (m_Animator)
            {
                m_OriginalController = m_Animator.runtimeAnimatorController;
            }
        }

        private void OnDisable()
        {
            // It's important that the original Materials that we pulled out of the renderers are put back.
            // Otherwise nothing will Destroy() them and they will leak! (Alternatively we could manually
            // Destroy() these in our OnDestroy(), but in this case it makes more sense just to put them back.)
            ClearOverrideMaterial();
        }

        /// <summary>
        /// Swap the visuals of the character to the index passed in.
        /// </summary>
        /// <param name="modelIndex">Zero-based array index of the model</param>
        /// <param name="specialMaterialMode">Special Material to apply to all body parts</param>
        public void SwapToModel(int modelIndex, SpecialMaterialMode specialMaterialMode = SpecialMaterialMode.None)
        {
            Assert.IsTrue(modelIndex < m_CharacterModels.Length);

            ClearOverrideMaterial();

            for (int i = 0; i < m_CharacterModels.Length; i++)
            {
                m_CharacterModels[i].SetFullActive(i == modelIndex);
                if (m_CharacterModels[i].specialFx)
                {
                    // Disable all the specialFx; we'll turn the correct one on after the loop.
                    // "Why not just turn on the specialFx right here?" you ask.
                    // Because the same specialFx object is used for both the male and female
                    // version of each class, so if we tried to turn them on/off here, we'd end up
                    // turning it both on AND off each time through the loop! (With 50/50 odds
                    // that it ends in the state we want it to be.)
                    m_CharacterModels[i].specialFx.enabled = false;
                }
            }

            if (m_CharacterModels[modelIndex].specialFx)
            {
                m_CharacterModels[modelIndex].specialFx.enabled = true;
            }

            if (m_Animator)
            {
                // plug in the correct animator override... or plug the original non - overridden version back in!
                if (m_CharacterModels[modelIndex].animatorOverrides)
                {
                    m_Animator.runtimeAnimatorController = m_CharacterModels[modelIndex].animatorOverrides;
                }
                else
                {
                    m_Animator.runtimeAnimatorController = m_OriginalController;
                }
            }

            // lastly, now that we're all assembled, apply any override material.
            switch (specialMaterialMode)
            {
                case SpecialMaterialMode.StealthySelf:
                    SetOverrideMaterial(modelIndex, m_StealthySelfMaterial);
                    break;
                case SpecialMaterialMode.StealthyOther:
                    SetOverrideMaterial(modelIndex, m_StealthyOtherMaterial);
                    break;
            }
        }

        private void ClearOverrideMaterial()
        {
            foreach (var entry in m_OriginalMaterials)
            {
                if (entry.Key)
                {
                    entry.Key.material = entry.Value;
                }
            }
            m_OriginalMaterials.Clear();
        }

        private void SetOverrideMaterial(int modelIdx, Material overrideMaterial)
        {
            ClearOverrideMaterial(); // just sanity-checking; this should already have been called!
            foreach (var bodypart in m_CharacterModels[modelIdx].GetAllBodyParts())
            {
                if (bodypart)
                {
                    m_OriginalMaterials[bodypart] = bodypart.material;
                    bodypart.material = overrideMaterial;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // if an Animator is on the same GameObject as us, assume that's the one we'll be using!
            if (!m_Animator)
                m_Animator = GetComponent<Animator>();
        }
#endif
    }
}
